using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using D.Utils;
using Microsoft.Extensions.Logging;

namespace D.FreeExchange
{
    public class ExchangeClientProxy
        : ExchangeProxy
        , IExchangeClientProxy
    {
        Dictionary<string, string> _session;

        public IDictionary<string, string> Session => _session;

        public ExchangeClientProxy(
            ILogger<ExchangeClientProxy> logger
            , ITransporter transporter
            , IExchangeProtocol protocol
            , IActionExecutor executor
            ) : base(logger)
        {
            _session = new Dictionary<string, string>();

            _executor = executor;
            _protocol = protocol;
            _transporter = transporter;
        }

        public override IResult Run()
        {
            return base.Run();
        }
    }
}
