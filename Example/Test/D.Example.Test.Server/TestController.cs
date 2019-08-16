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

        public IResult<string> EchoMessage(string msg)
        {
            return Result.CreateSuccess<string>(msg);
        }
    }
}
