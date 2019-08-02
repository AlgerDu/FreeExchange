using System;
using System.Collections.Generic;
using System.Text;

namespace D.FreeExchange.Protocol.DP
{
    /// <summary>
    /// IProtocolPayload 解析器
    /// </summary>
    public interface IPayloadAnalyser
    {
        /// <summary>
        /// 将 IProtocolPayload 解析为顺序的包数组
        /// </summary>
        /// <param name="payload"></param>
        /// <returns></returns>
        IEnumerable<IPackage> Analyse(IProtocolPayload payload);
    }
}
