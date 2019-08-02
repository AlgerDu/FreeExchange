using System;
using System.Collections.Generic;
using System.Text;

namespace D.FreeExchange.Protocol.DP
{
    public interface IPackageFactory
    {
        IPackage CreatePackage(byte headBuffer);
    }
}
