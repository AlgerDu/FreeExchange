using System;
using System.Collections.Generic;
using System.Text;

namespace D.FreeExchange
{
    public class ByteDescription : IByteDescription
    {
        public string Path { get; set; }
        public byte[] Buffer { get; set; }
    }

    public class ProtocolPayload : IProtocolPayload
    {
        public string Text { get; set; }

        public IEnumerable<IByteDescription> Bytes { get; set; }
    }
}
