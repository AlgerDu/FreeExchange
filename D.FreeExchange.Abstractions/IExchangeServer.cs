using System;
using System.Collections.Generic;
using System.Text;

namespace D.FreeExchange
{
    /// <summary>
    /// 服务
    /// </summary>
    public interface IExchangeServer
    {
        /// <summary>
        /// 所有连入的客户端代理
        /// </summary>
        IDictionary<Guid, IExchangeClientProxy> ClientProxies { get; }

        /// <summary>
        /// 启动
        /// </summary>
        /// <returns></returns>
        IExchangeServer Run();
    }
}
