using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace D.FreeExchange.Core.Interface
{
    /// <summary>
    /// 上下文
    /// TODO：不该暴露的 set 属性，如何在不同的情况下不暴露出去
    /// </summary>
    public interface IContext
    {
        /// <summary>
        /// 唯一标志
        /// </summary>
        Guid Uid { get; set; }

        /// <summary>
        /// 所署的客户端
        /// </summary>
        Guid ClientUid { get; set; }

        /// <summary>
        /// context 状态
        /// </summary>
        ContextState State { get; set; }

        /// <summary>
        /// action 的路由
        /// </summary>
        string Url { get; set; }

        /// <summary>
        /// 重发次数
        /// </summary>
        int ResendTimes { get; set; }

        /// <summary>
        /// 请求数据
        /// </summary>
        object RequestData { get; set; }

        /// <summary>
        /// 回复数据
        /// </summary>
        object ResponseData { get; set; }
    }
}
