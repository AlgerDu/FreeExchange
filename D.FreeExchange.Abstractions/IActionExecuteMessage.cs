using System;
using System.Collections.Generic;
using System.Text;

namespace D.FreeExchange
{
    public interface IActionExecuteMessage : IExchangeMessage
    {
        IExchangeProxy Proxy { get; }

        object Response { get; }

        IEnumerable<IByteDescription> ByteDescriptions { get; }
    }
}
