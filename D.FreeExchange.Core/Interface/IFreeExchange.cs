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
        /// 和连接的另外一端交换数据
        /// </summary>
        /// <typeparam name="T">返回的数据</typeparam>
        /// <param name="url">路由</param>
        /// <param name="data">发送的数据</param>
        /// <param name="hasBytes">发送的数据中是否有 byte 类型的字段</param>
        /// <returns></returns>
        Task<T> Exchange<T>(string url, object data, bool hasBytes = false) where T : class;
    }

    public delegate void OnExchange();
}
