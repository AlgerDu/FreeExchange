using D.FreeExchange;
using D.Infrastructures;
using D.Utils;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace D.Example.Test
{
    public class TestClientApp : IApplication
    {
        ILogger _logger;
        ServerProxy _server;

        public TestClientApp(
            ILogger<TestClientApp> logger
            , ServerProxy server
            )
        {
            _logger = logger;
            _server = server;
        }

        public IApplication Run()
        {
            _logger.LogInformation($"{this} start running");

            _server.Connect();

            TestSend();

            return this;
        }

        public IApplication Stop()
        {
            return this;
        }

        private void TestSend()
        {
            Task.Run(() =>
            {
                System.Threading.Thread.Sleep(2000);

                _logger.LogInformation($"开始尝试调用接口 Test/SendMessage");

                _server.SendAsync<Result>(new ExchangeMessage
                {
                    Url = "Test/SendMessage",
                    Params = new object[] { "你好" },
                    Timeout = TimeSpan.FromSeconds(20)
                })
                .ContinueWith((r) =>
                {
                    _logger.LogInformation($"尝试调用接口结果：{r}");
                });
            });
        }
    }
}
