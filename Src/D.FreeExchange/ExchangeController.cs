using System;
using System.Collections.Generic;
using System.Text;

namespace D.FreeExchange
{
    public class ExchangeController : IExchangeController
    {
        public IExchangeProxy Proxy { get; private set; }
    }
}
