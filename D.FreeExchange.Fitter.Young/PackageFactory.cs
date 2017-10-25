using D.FreeExchange.Core.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace D.FreeExchange.Fitter.Young
{
    /// <summary>
    /// 包生产
    /// </summary>
    internal class PackageFactory
    {
        /// <summary>
        /// 通过不断的压入 buffer ，来完成一个 package 的构建
        /// </summary>
        /// <param name="package"></param>
        /// <param name="buffer"></param>
        /// <param name="offest"></param>
        /// <returns></returns>
        public bool Create(out Package package, byte[] buffer, ref int offest)
        {
            package = null;
            return false;
        }

        /// <summary>
        /// 将一个 IProductPart 拆分成一个或者多个 buffer 数据包
        /// </summary>
        /// <param name="product"></param>
        /// <returns></returns>
        public IEnumerable<byte[]> Create(IProductPart product)
        {
            return null;
        }
    }
}
