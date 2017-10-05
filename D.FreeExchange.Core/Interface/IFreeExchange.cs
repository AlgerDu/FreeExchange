using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace D.FreeExchange.Core.Interface
{
    /// <summary>
    /// 自由交换
    /// </summary>
    public interface IFreeExchange
    {
        /// <summary>
        /// 唯一标志
        /// </summary>
        Guid Uid { get; }

        /// <summary>
        /// 和连接的另外一端交换数据，发送请求
        /// </summary>
        /// <typeparam name="R">返回的数据类型</typeparam>
        /// <typeparam name="D">传入的数据类型</typeparam>
        /// <param name="url">路由</param>
        /// <param name="data">发送的数据</param>
        /// <param name="hasBytes">发送的数据中是否有 byte 类型的字段</param>
        /// <returns></returns>
        Task<R> Exchange<R, D>(string url, D data, bool hasBytes = false);

        /// <summary>
        /// 回复连接的另外一端的请求数据
        /// </summary>
        /// <typeparam name="D">回复请求的数据类型</typeparam>
        /// <param name="uid">请求的唯一标识</param>
        /// <param name="data">回复的数据</param>
        /// <returns>是否成功</returns>
        Task<bool> Exchange<D>(Guid uid, D data);

        /// <summary>
        /// 当收到另一端的连接请求的时候，供外部使用处理请求
        /// </summary>
        event OnExchangeHandler OnExchange;
    }
}
