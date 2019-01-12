using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using D.Utils;
using Microsoft.Extensions.Logging;

namespace D.FreeExchange
{
    public class DProtocolBuilder : IProtocolBuilder
    {
        ILogger _logger;

        ITransporter _transporter;

        public DProtocolBuilder(
            ILogger<DProtocolBuilder> logger
            , ITransporter transporter
            )
        {
            _logger = logger;
            _transporter = transporter;
        }

        #region IProtocolBuilder 实现
        public IProtocolBuilder Run()
        {
            throw new NotImplementedException();
        }

        public Task<IResult> SendAsync(IProtocolPayload payload)
        {
            throw new NotImplementedException();
        }

        public void SetControlReceiveAction(Action<int> action)
        {
            throw new NotImplementedException();
        }

        public void SetPayloadReceiveAction(Action<IProtocolPayload> action)
        {
            throw new NotImplementedException();
        }

        public IProtocolBuilder Stop()
        {
            throw new NotImplementedException();
        }
        #endregion

        #region 发送，暂时先都写在这里，写完在考虑封装整理

        #endregion

        #region 接收，和发送一样

        #endregion
    }
}
