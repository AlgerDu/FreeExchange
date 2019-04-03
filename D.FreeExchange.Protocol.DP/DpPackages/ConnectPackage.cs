using System;
using System.Collections.Generic;
using System.Text;

namespace D.FreeExchange.Protocol.DP
{
    public class ConnectPackage : PackageHeader
    {
        public ConnectPackage(
            IPackage header
            )
            : base(header)
        {

        }
    }
}
