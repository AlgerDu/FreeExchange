using System;
using System.Collections.Generic;
using System.Text;

namespace D.FreeExchange
{
    /// <summary>
    /// 思考了很久，觉得还是这样比较好
    /// </summary>
    public interface IActionExecuteMessage
    {
        /// <summary>
        /// 请求的路由
        /// </summary>
        string Url { get; }

        /// <summary>
        /// 超时时间
        /// </summary>
        TimeSpan? Timeout { get; }

        /// <summary>
        /// 发起请求的 proxy
        /// </summary>
        IExchangeProxy Proxy { get; }

        /// <summary>
        /// 请求参数的 json 字符串
        /// </summary>
        string Request { get; set; }

        /// <summary>
        /// response 中的 json 字符串
        /// </summary>
        string Response { get; set; }

        /// <summary>
        /// 请求和回复模型中的 byte 数组
        /// </summary>
        IEnumerable<IByteDescription> ByteDescriptions { get; }
    }
}
