using System;
using System.Collections.Generic;
using System.Text;

namespace D.FreeExchange.Protocol.DP
{
    /// <summary>
    /// 包工厂
    /// </summary>
    public interface IPackageFactory
    {
        /// <summary>
        /// 通过一个字节来确定具体的包
        /// </summary>
        /// <param name="headBuffer"></param>
        /// <returns></returns>
        IPackage CreatePackage(byte headBuffer);
    }
}
