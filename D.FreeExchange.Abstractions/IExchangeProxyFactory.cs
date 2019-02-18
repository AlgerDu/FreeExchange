using System;
using System.Collections.Generic;
using System.Text;

namespace D.FreeExchange
{
    public interface IExchangeProxyFactory
    {
        IExchangeProxy Create(string address, string protocol, string transpor);
    }
}