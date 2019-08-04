using System;
using System.Collections.Generic;
using System.Text;

namespace D.FreeExchange.Protocol.DP
{
    /// <summary>
    /// 心跳相关
    /// </summary>
    public interface IDProtocolHeart
    {
        /// <summary>
        /// 处理收到的心跳包
        /// </summary>
        /// <param name="package"></param>
        void DealHerat(IPackage package);
    }
}
