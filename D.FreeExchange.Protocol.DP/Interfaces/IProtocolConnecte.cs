using System;
using System.Collections.Generic;
using System.Text;

namespace D.FreeExchange.Protocol.DP
{
    /// <summary>
    /// 连接相关
    /// </summary>
    public interface IProtocolConnecte
    {
        /// <summary>
        /// 处理连接相关的协议包
        /// </summary>
        /// <param name="package"></param>
        void DealPackage(IPackage package);
    }
}
