using System;
using System.Collections.Generic;
using System.Text;

namespace D.FreeExchange.Protocol.DP
{
    /// <summary>
    /// 处理接收到带有 index 的包；
    /// 基本是和 send 相反的
    /// </summary>
    public interface IProtocolReceive
    {
        /// <summary>
        /// 处理接收到的 index 包
        /// </summary>
        /// <param name="package"></param>
        void DealIndexPackage(IPackage package);
    }
}
