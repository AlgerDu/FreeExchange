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
        Guid Uid { get; set; }

        /// <summary>
        /// action 的路由
        /// </summary>
        string Url { get; set; }

        /// <summary>
        /// 执行结果
        /// </summary>
        ContextState State { get; set; }
    }
}
