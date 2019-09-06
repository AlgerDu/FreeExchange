using Autofac;
using D.FreeExchange;
using D.FreeExchange.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;

namespace D.Example.Test
{
    public class ServerProxy : ExchangeServerProxy
    {
        ILogger _logger;
        ServerProxyOptions _options;
        ILifetimeScope _scope;

        public ServerProxy(
            ILogger<ServerProxy> logger
            , IOptionsSnapshot<ServerProxyOptions> options
            , ILifetimeScope scope
            )
        {
            _logger = logger;
            _options = options.Value;
            _scope = scope;
        }

        private void CreateProxy()
        {
            var transpoter = _scope.ResolveUdpServerProxyTransporter(_options.Address);

            var protocol = _scope.ResolveDProtocol(ExchangeProtocolRunningMode.Client);

            var proxy = _scope.Resolve<UdpExchangeServerProxy>(
                new TypedParameter(typeof(IExchangeProtocol), transpoter)
                );

            proxy.UpdateTransporter(transpoter);
        }
    }
}
