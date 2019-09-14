using System;
using System.Collections.Generic;
using System.Text;

namespace D.FreeExchange
{
    public class ExchangeClientConnecttedEventArgs
    {
        public IExchangeClientProxy NewClient { get; set; }

        public DateTimeOffset Timestamp { get; set; }
    }
}
