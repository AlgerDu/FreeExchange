using System;
using System.Collections.Generic;
using System.Text;

namespace D.FreeExchange
{
    /// <summary>
    /// 字节数据
    /// </summary>
    public interface IByteDescription
    {
        string Path { get; set; }

        byte[] Buffer { get; set; }
    }

    /// <summary>
    /// 协议携带的数据
    /// </summary>
    public interface IProtocolPayload
    {
        string Text { get; }

        IEnumerable<IByteDescription> Bytes { get; }
    }
}
