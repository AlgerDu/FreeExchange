using System;
using System.Collections.Generic;
using System.Text;

namespace D.FreeExchange
{
    /// <summary>
    /// 客户端代理
    /// </summary>
    public interface IExchangeClientProxy : IExchangeProxy
    {
        /// <summary>
        /// Session 服务
        /// </summary>
        IDictionary<string, string> Session { get; }
    }
}
