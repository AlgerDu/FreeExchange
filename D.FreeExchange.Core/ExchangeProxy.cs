using System;
using System.Collections.Generic;
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

        protected string _Address;
        protected Guid _uid;

        protected Dictionary<Guid, ExchangeMessageCache> _sendMsgCaches;

        #region ExchangeProxy 属性
        public Guid Uid => _uid;

        public bool Online => true;

        public string Address => _Address;
        #endregion

        public ExchangeProxy(
            ILogger logger
            , string address
            , IProtocolBuilder protocol
            )
        {
            _logger = logger;

            _Address = address;
            _protocol = protocol;

            _sendMsgCaches = new Dictionary<Guid, ExchangeMessageCache>();
        }

        #region ExchangeProxy 行为
        public Task<IResult> Disconnect()
        {
            return _protocol.Stop();
        }

        public Task<T> SendAsync<T>(IExchangeMessage msg) where T : IResult, new()
        {
            return Task.Run<T>(() =>
            {
                var cache = new ExchangeMessageCache(msg);
                _sendMsgCaches.Add(cache.Uid, cache);

                MsgToPayload(new ExchangeMessageForPayload
                {
                    Uid = cache.Uid,
                    Params = cache.Params,
                    Url = cache.Url,
                    State = cache.State
                });

                var timeout = !cache.TCS.Task.Wait(msg.Timeout);

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

                return (T)cache.Response;
            });
        }
        #endregion

        private async void SendCacheMsg(ExchangeMessageCache cache)
        {
            cache.State = ExchangeMessageState.Sending;

            var jsonStr = JsonConvert.SerializeObject(cache);

            var payload = new ProtocolPayload
            {
                Text = jsonStr
            };

            var proRst = await _protocol.SendAsync(payload);

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
