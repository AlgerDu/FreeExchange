using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using D.Utils;
using Microsoft.Extensions.Logging;

namespace D.FreeExchange
{
    /// <summary>
    /// 代理基类
    /// </summary>
    public abstract class ExchangeProxy : IExchangeProxy
    {
        protected ILogger _logger;
        protected IExchangeProtocol _protocol;
        protected IActionExecutor _executor;
        protected ITransporter _transporter;

        protected string _Address;
        protected Guid _uid;

        protected Dictionary<Guid, ExchangeMessageCache> _sendMsgCaches;

        #region ExchangeProxy 属性

        public event ExchangeProxyBackOnlineEventHandler BackOnline;
        public event ExchangeProxyOfflineEventHandler Offline;
        public event ExchangeProxyConnectedEventHandler Connected;
        public event ExchangeProxyClosingEventHandler Closing;

        public Guid Uid => _uid;

        public bool Online => true;

        public string Address => _transporter?.Address;
        #endregion

        public ExchangeProxy(
            ILogger logger
            )
        {
            _logger = logger;

            _sendMsgCaches = new Dictionary<Guid, ExchangeMessageCache>();
        }

        #region ExchangeProxy 行为

        public virtual Task<IResult> Disconnect()
        {
            _transporter.Close();
            return _protocol.Stop();
        }

        public virtual Task<T> SendAsync<T>(IExchangeMessage msg) where T : IResult, new()
        {
            return Task.Run<T>(() =>
            {
                var cache = AnalyseRequestAndCache(msg);

                SendRequestMsg(cache);

                var timeout = !cache.TCS.Task.Wait(msg.Timeout);

                // 发送超时
                if (timeout)
                {
                    if (cache.State <= ExchangeMessageState.Sending)
                    {
                        _logger.LogTrace($"{this} {cache.Uid} 发送超时 {cache.Timeout}");
                        return CreateErrorResult<T>((int)ExchangeCode.SentTimeout);
                    }
                    else
                    {
                        cache.TCS = new TaskCompletionSource<IResult>();

                        timeout = !cache.TCS.Task.Wait(msg.Timeout);
                    }
                }

                //接收返回结果超时
                if (timeout)
                {
                    if (cache.State <= ExchangeMessageState.ProcessTimeout)
                    {
                        _logger.LogTrace($"{this} {cache.Uid} 接收超时 {cache.Timeout}");
                        return CreateErrorResult<T>((int)ExchangeCode.ReceivceTimeout);
                    }
                }

                var taskRst = cache.TCS.Task.Result;

                if (!taskRst.IsSuccess())
                {
                    return CreateErrorResult<T>(taskRst.Code, taskRst.Msg);
                }

                try
                {
                    //TODO 解析返回值
                    return AnalyeResponse<T>(cache);
                }
                catch (Exception ex)
                {
                    return CreateErrorResult<T>((int)ExchangeCode.ResponseTypeError, ex.ToString());
                }
            });
        }
        #endregion

        public virtual IResult Run()
        {
            _protocol.SetReceivedPayloadAction(this.ProtocolReceivePayload);
            _protocol.SetReceivedCmdAction(this.ProtocolReceiveControl);
            _protocol.SetSendBufferAction(this.SendBufferAction);

            _transporter.SetReceiveAction(this.TransporterReceivedBuffer);

            _protocol.Run();
            _transporter.Connect();

            return Result.CreateSuccess();
        }

        #region 接收

        private void TransporterReceivedBuffer(byte[] buffer, int offset, int length)
        {
            _protocol.PushBuffer(buffer, offset, length);
        }

        private void ProtocolReceiveControl(int cmd, DateTimeOffset time)
        {

        }

        private void SendBufferAction(byte[] buffer, int offest, int length)
        {
            _transporter.SendAsync(buffer, offest, length);
        }

        private void ProtocolReceivePayload(IProtocolPayload payload)
        {
            var msg = JsonConvert.DeserializeObject<ExchangeMessageForPayload>(payload.Text);

            switch (msg.State)
            {
                case ExchangeMessageState.Sending:
                    DealSendingMsg(msg);
                    break;

                case ExchangeMessageState.Recevied:
                    DealReceviedMsg(msg);
                    break;

                case ExchangeMessageState.Complete:
                    DealCompleteMsg(msg);
                    break;

                default:
                    _logger.LogWarning($"{msg.Uid} 暂时不处理的 message state {msg.State}");
                    break;
            }
        }

        private void DealSendingMsg(ExchangeMessageForPayload msg)
        {
            _logger.LogTrace($"{this} 接收到请求 {msg.Uid} {msg.Url}");

            SendRecevieResponse(msg);

            var executeRst = _executor.InvokeAction(new ActionExecuteMessage
            {
                Url = msg.Url,
                Params = msg.RequestJsonStrs,
                Timeout = msg.Timeout.HasValue ? msg.Timeout.Value : TimeSpan.FromMinutes(1)
            });

            //TODO 上面有问题

            var response = new ExchangeMessageForPayload();

            if (executeRst.IsSuccess())
            {
                response.Code = ExchangeCode.OK;
                response.ResponseJsonStr = JsonConvert.SerializeObject(executeRst.Data);
                response.State = ExchangeMessageState.Complete;
                response.Uid = msg.Uid;
            }
            else
            {
                response.Code = ExchangeCode.ActionExecuteError;
                response.State = ExchangeMessageState.Complete;
                response.Uid = msg.Uid;
                response.Msg = executeRst.Msg;
            }

            SendMsg(response);
        }

        private void DealReceviedMsg(ExchangeMessageForPayload msg)
        {
            _logger.LogInformation($"{this} 消息 {msg.Uid} 对方已经收到");

            if (_sendMsgCaches.ContainsKey(msg.Uid.Value))
            {
                var cache = _sendMsgCaches[msg.Uid.Value];

                cache.State = ExchangeMessageState.Processing;
            }
            else
            {
                _logger.LogWarning($"收到 ExchangeMessageState.Recevied 回复，但是 {msg.Uid} 已经不在缓存中");
            }
        }

        private void DealCompleteMsg(ExchangeMessageForPayload msg)
        {
            _logger.LogInformation($"{this} 消息 {msg.Uid} 对方已经处理完成");

            if (_sendMsgCaches.ContainsKey(msg.Uid.Value))
            {
                var cache = _sendMsgCaches[msg.Uid.Value];

                cache.State = ExchangeMessageState.Complete;
                cache.ResponseJsonStr = msg.ResponseJsonStr;

                cache.TCS.SetResult(Result.CreateSuccess());
            }
            else
            {
                _logger.LogWarning($"收到 ExchangeMessageState.Complete 回复，但是 {msg.Uid} 已经不在缓存中");
            }
        }

        /// <summary>
        /// 发送收到的回复
        /// </summary>
        /// <param name="msg"></param>
        private void SendRecevieResponse(ExchangeMessageForPayload msg)
        {
            var response = new ExchangeMessageForPayload
            {
                Uid = msg.Uid,
                State = ExchangeMessageState.Recevied
            };

            SendMsg(response);
        }

        #endregion

        /// <summary>
        /// 将请求参数封装为 json 数据
        /// TODO byte[] 型数据尚未处理
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        private ExchangeMessageCache AnalyseRequestAndCache(IExchangeMessage msg)
        {
            var cache = new ExchangeMessageCache()
            {
                Timeout = msg.Timeout,
                Url = msg.Url,
                State = ExchangeMessageState.Create
            };

            List<string> jsons = new List<string>(msg.Params.Length);

            foreach (var p in msg.Params)
            {
                jsons.Add(JsonConvert.SerializeObject(p));
            }

            //TODO 解析参数中的字节部分

            cache.RequestJsonStrs = jsons.ToArray();

            _logger.LogTrace($"{this} 创建请求 {cache.Uid} {cache.Url}，参数个数 {cache.RequestJsonStrs.Length}");

            _sendMsgCaches.Add(cache.Uid.Value, cache);

            return cache;
        }

        private T AnalyeResponse<T>(ExchangeMessageCache cache)
        {
            return JsonConvert.DeserializeObject<T>(cache.ResponseJsonStr);
        }

        private Task<IResult> SendMsg(ExchangeMessageForPayload msg)
        {
            var payload = new ProtocolPayload
            {
                Bytes = msg.ByteDescriptions
            };

            msg.ByteDescriptions = null;
            var jsonStr = JsonConvert.SerializeObject(msg);

            payload.Text = jsonStr;

            _logger.LogTrace($"SendMsg {msg.Uid} {msg.State}");

            return _protocol.PushPayload(payload);
        }

        private async void SendRequestMsg(ExchangeMessageCache cache)
        {
            cache.State = ExchangeMessageState.Sending;

            var msg = new ExchangeMessageForPayload
            {
                Uid = cache.Uid,
                Url = cache.Url,
                RequestJsonStrs = cache.RequestJsonStrs,
                ByteDescriptions = cache.ByteDescriptions,
                State = cache.State,
                Timeout = cache.Timeout
            };

            var proRst = await SendMsg(msg);

            if (!proRst.IsSuccess())
            {
                cache.TCS.SetResult(Result.CreateError((int)ExchangeCode.SentBufferFull));
            }
        }

        /// <summary>
        /// TODO 后面需要找个时间迁移到 D.Utils.Result 里面
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="code"></param>
        /// <param name="msg"></param>
        /// <returns></returns>
        private T CreateErrorResult<T>(int code, string msg = null) where T : IResult, new()
        {
            var type = typeof(T);
            var tmpRst = (T)Activator.CreateInstance(type);

            var p = type.GetProperty("Code");
            p.SetValue(tmpRst, code);

            p = type.GetProperty("Msg");
            p.SetValue(tmpRst, msg);

            return tmpRst;
        }
    }
}
