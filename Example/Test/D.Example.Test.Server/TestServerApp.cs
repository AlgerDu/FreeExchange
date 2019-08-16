using D.Infrastructures;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace D.Example.Test
{
    public class TestServerApp : IApplication
    {
        ILogger _logger;
        ExchangeServerHost _host;

        public TestServerApp(
            ILogger<TestServerApp> logger
            , ExchangeServerHost host
            )
        {
            _logger = logger;
            _host = host;
        }

        public IApplication Run()
        {
            _host.Run();

            return this;
        }

        public IApplication Stop()
        {
            _host.Stop();

            return this;
        }
    }
}
