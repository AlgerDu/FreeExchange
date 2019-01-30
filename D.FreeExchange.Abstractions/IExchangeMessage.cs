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
        string Url { get; }

        object[] Params { get; }

        TimeSpan Timeout { get; }
    }
}
