using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using D.Utils;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace D.FreeExchange.Core
{
    public abstract class ExchangeProxy : IExchangeProxy
    {
        protected ILogger _logger;
        protected IProtocolBuilder _protocol;
        protected IActionExecutor _executor;
        protected ITransporter _transporter;

        protected string _Address;
        protected Guid _uid;

        protected Dictionary<Guid, ExchangeMessageCache> _sendMsgCaches;

        #region ExchangeProxy 属性
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

        protected virtual IResult Run()
        {
            _protocol.SetReceivedPayloadAction(this.ProtocolReceivePayload);
            _protocol.SetReceivedControlAction(this.ProtocolReceiveControl);
            _protocol.SetSendBufferAction(this.SendBufferAction);

            _transporter.SetReceiveAction(this.TransporterReceivedBuffer);

            return Result.CreateSuccess();
        }

        #region 接收

        private void TransporterReceivedBuffer(byte[] buffer, int offset, int length)
        {
            _protocol.PushBuffer(buffer, offset, length);
        }

        private void ProtocolReceiveControl(int ctlCode)
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
            SendRecevieResponse(msg);

            var executeRst = _executor.InvokeAction(new ExchangeMessage
            {
                Url = msg.Url,
                Params = new object[] { msg.ResponseJsonStr },
                Timeout = msg.Timeout.Value
            }, null);
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
                response.State = ExchangeMessageState.Processing;
                response.Uid = msg.Uid;
                response.Msg = executeRst.Msg;
            }

            SendMsg(response);
        }

        private void DealReceviedMsg(ExchangeMessageForPayload msg)
        {
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

        private ExchangeMessageCache AnalyseRequestAndCache(IExchangeMessage msg)
        {
            var cache = new ExchangeMessageCache()
            {
                Timeout = msg.Timeout,
                Url = msg.Url
            };

            cache.RequestJsonStr = JsonConvert.SerializeObject(msg.Params);

            _sendMsgCaches.Add(cache.Uid.Value, cache);

            return cache;
        }

        private T AnalyeResponse<T>(ExchangeMessageCache cache)
        {
            return JsonConvert.DeserializeObject<T>(cache.ResponseJsonStr);
        }

        private Task<IResult> SendMsg(ExchangeMessageForPayload msg)
        {
            var jsonStr = JsonConvert.SerializeObject(msg);

            var payload = new ProtocolPayload
            {
                Text = jsonStr
            };

            return _protocol.PushPayload(payload);
        }

        private async void SendRequestMsg(ExchangeMessageCache cache)
        {
            cache.State = ExchangeMessageState.Sending;

            var msg = new ExchangeMessageCache
            {
                Uid = cache.Uid,
                Url = cache.Url,
                RequestJsonStr = cache.RequestJsonStr,
                ByteDescriptions = cache.ByteDescriptions,
                State = cache.State
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
