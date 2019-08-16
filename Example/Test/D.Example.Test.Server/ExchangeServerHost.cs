using Autofac;
using D.FreeExchange.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;

namespace D.Example.Test
{
    /// <summary>
    /// host server 的承载
    /// </summary>
    public class ExchangeServerHost
    {
        ILogger _logger;
        ServerHostOptions _options;

        UdpExchangeServer _server;

        public ExchangeServerHost(
            ILogger<ExchangeServerHost> logger
            , IOptionsSnapshot<ServerHostOptions> options
            , ILifetimeScope scope
            )
        {
            _options = options.Value;

            _server = scope.Resolve<UdpExchangeServer>(new TypedParameter(typeof(int), _options.ListenPort));
        }

        public void Run()
        {
            _server.Run();
        }

        public void Stop()
        {
            //TODO 还没有停止的接口
        }
    }
}
