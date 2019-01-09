using D.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace D.FreeExchange
{
    /// <summary>
    /// 交换的信息
    /// </summary>
    public interface IExchangeMessage
    {
        Guid Uid { get; }

        IEnumerable<object> Request { get; }

        object Response { get; }

        TimeSpan SendTimeout { get; }

        TimeSpan ReceiveTimeout { get; }
    }
}
