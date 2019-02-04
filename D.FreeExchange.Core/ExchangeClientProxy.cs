using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Microsoft.Extensions.Logging;

namespace D.FreeExchange.Core
{
    public class ExchangeClientProxy
        : ExchangeProxy
        , IExchangeClientProxy
    {
        Dictionary<string, string> _session;

        public IDictionary<string, string> Session => _session;

        public IPEndPoint EndPoint { get; private set; }

        public ExchangeClientProxy(
            ILogger<ExchangeClientProxy> logger
            , IPEndPoint endPoint
            , IProtocolBuilder protocol
            , IActionExecutor executor
            ) : base(logger, endPoint.ToString(), protocol, executor)
        {
            EndPoint = endPoint;

            _session = new Dictionary<string, string>();
        }
    }
}
