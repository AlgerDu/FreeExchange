using System;
using System.Collections.Generic;
using System.Text;

namespace D.FreeExchange.Protocol.DP
{
    /// <summary>
    /// 连接成功包
    /// </summary>
    internal class ConnectOkPackage : PackageHeader
    {
        public ConnectOkPackage()
            : base(PackageCode.ConnectOK)
        {

        }

        public ConnectOkPackage(IPackage header)
            : base(header)
        {

        }
    }
}
