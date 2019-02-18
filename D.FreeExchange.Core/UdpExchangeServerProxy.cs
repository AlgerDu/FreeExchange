using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using D.Utils;
using Microsoft.Extensions.Logging;

namespace D.FreeExchange.Core
{
    public class UdpExchangeServerProxy
        : ExchangeProxy
        , IExchangeServerProxy
    {
        public UdpExchangeServerProxy(
            ILogger logger
            , string address
            , IProtocolBuilder protocol
            , IActionExecutor executor
            ) : base(logger, address, protocol, executor)
        {
        }

        public Task<IResult> Connect()
        {
            throw new NotImplementedException();
        }

        public IResult UpdateAddress(string newAddress)
        {
            throw new NotImplementedException();
        }
    }
}
