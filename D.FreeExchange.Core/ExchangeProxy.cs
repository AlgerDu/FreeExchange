using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using D.Utils;
using Microsoft.Extensions.Logging;

namespace D.FreeExchange.Core
{
    public abstract class ExchangeProxy : IExchangeProxy
    {
        protected ILogger _logger;
        protected IProtocolBuilder _protocol;

        protected string _Address;
        protected Guid _uid;

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
        }

        #region ExchangeProxy 行为
        public Task<IResult> Disconnect()
        {
            return _protocol.Stop();
        }

        public Task<IResult> SendAsync(string url, object[] param, TimeSpan sendTimeout, TimeSpan receiveTimeout)
        {
            throw new NotImplementedException();
        }

        public Task<IResult<T>> SendAsync<T>(string url, object[] param, TimeSpan sendTimeout, TimeSpan receiveTimeout) where T : class, new()
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}
