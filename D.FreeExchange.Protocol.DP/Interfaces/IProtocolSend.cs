using D.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace D.FreeExchange.Protocol.DP
{
    /// <summary>
    /// 协议的发送部分；
    /// 处理所有有 index 的包的发送；重发，清理等等
    /// </summary>
    public interface IProtocolSend
    {
        /// <summary>
        /// 发送需要分配 xindex 的包
        /// </summary>
        /// <param name="packages"></param>
        /// <returns></returns>
        IResult DistributeThenSendPackages(IEnumerable<IPackage> packages);

        /// <summary>
        /// 收到了回复包，处理
        /// </summary>
        /// <param name="package"></param>
        void DealAnswer(IPackage package);
    }
}
