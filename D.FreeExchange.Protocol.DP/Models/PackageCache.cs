using System;
using System.Collections.Generic;
using System.Text;

namespace D.FreeExchange.Protocol.DP.Models
{
    /// <summary>
    /// 包缓存项
    /// </summary>
    public class PackageCacheItem
    {
        public PackageState State { get; set; }

        public IPackage Package { get; set; }
    }
}
