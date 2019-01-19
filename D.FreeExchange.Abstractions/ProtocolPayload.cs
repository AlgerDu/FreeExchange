using System;
using System.Collections.Generic;
using System.Text;

namespace D.FreeExchange
{
    public class ProtocolPayload : IProtocolPayload
    {
        public string Text { get; set; }

        public IEnumerable<IByteDescription> Bytes { get; set; }
    }
}
