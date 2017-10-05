using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace D.FreeExchange.Core.Interface
{
    /// <summary>
    /// 上下文
    /// </summary>
    public interface IContext
    {
        /// <summary>
        /// 唯一标志
        /// </summary>
        Guid Uid { get; }

        string Url { get; }
    }
}
