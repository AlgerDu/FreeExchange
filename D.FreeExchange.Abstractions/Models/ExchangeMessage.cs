using System;
using System.Collections.Generic;
using System.Text;

namespace D.FreeExchange
{
    public class ExchangeMessage : IExchangeMessage
    {
        public string Url { get; set; }

        public object[] Params { get; set; }

        public TimeSpan Timeout { get; set; }
    }

    public class ActionExecuteMessage : ExchangeMessage, IActionExecuteMessage
    {
        public IExchangeProxy Proxy { get; set; }

        public IEnumerable<IByteDescription> ByteDescriptions { get; set; }
    }
}
