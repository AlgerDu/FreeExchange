using System;
using System.Collections.Generic;
using System.Text;

namespace D.FreeExchange
{
    public interface IExchangeController
    {
        IExchangeProxy Proxy { get; }
    }
}
