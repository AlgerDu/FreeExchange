using D.FreeExchange;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace D.Example.LiveChat.Server.Controllers
{
    public class ChattingController : ExchangeController
    {
        ILogger _logger;

        public ChattingController(
            ILogger<ChattingController> logger
            )
        {
            _logger = logger;
        }
    }
}
