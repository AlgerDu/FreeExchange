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
            ILogger<UdpExchangeServerProxy> logger
            , IExchangeProtocol protocol
            , IActionExecutor executor
            ) : base(logger)
        {
            _protocol = protocol;
            _executor = executor;
        }

        public Task<IResult> Connect()
        {
            return Task.Run(() =>
            {
                return base.Run();
            });
        }

        public IResult UpdateTransporter(ITransporter transporter)
        {
            _transporter = transporter;

            return Result.CreateSuccess();
        }
    }
}
