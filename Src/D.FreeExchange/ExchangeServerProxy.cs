using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using D.Utils;

namespace D.FreeExchange
{
    /// <summary>
    /// 
    /// </summary>
    public abstract class ExchangeServerProxy
        : IExchangeServerProxy
    {
        IExchangeServerProxy _proxy;

        public ExchangeServerProxy() { }

        #region IExchangeServerProxy
        public Guid Uid => _proxy.Uid;

        public bool Online => _proxy.Online;

        public string Address => _proxy.Address;

        public virtual Task<IResult> Connect()
        {
            return _proxy.Connect();
        }

        public virtual Task<IResult> Disconnect()
        {
            return _proxy.Disconnect();
        }

        public virtual Task<T> SendAsync<T>(IExchangeMessage msg) where T : IResult, new()
        {
            return _proxy.SendAsync<T>(msg);
        }

        public virtual IResult UpdateTransporter(ITransporter transporter)
        {
            return _proxy.UpdateTransporter(transporter);
        }
        #endregion

        protected void UpdateRealServerProxy(IExchangeServerProxy proxy)
        {
            _proxy = proxy;
        }
    }
}
