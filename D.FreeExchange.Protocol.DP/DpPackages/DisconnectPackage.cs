using System;
using System.Collections.Generic;
using System.Text;

namespace D.FreeExchange.Protocol.DP
{
    /// <summary>
    /// 断开连接包
    /// </summary>
    public class DisconnectPackage : PackageHeader
    {
        public DisconnectPackage()
            : base(PackageCode.Disconnect)
        {
        }
    }
}
