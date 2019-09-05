using D.FreeExchange;
using D.Utils;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace D.Example.Test
{
    public class TestController : ExchangeController
    {
        ILogger _logger;

        public TestController(
            ILogger<TestController> logger
            )
        {
            _logger = logger;
        }

        public IResult SendMessage(string msg)
        {
            _logger.LogInformation($"接收到消息 {msg}");
            return Result.CreateSuccess();
        }
    }
}
