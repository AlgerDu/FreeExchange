using D.FreeExchange.Core.Interface;
using D.Util.Interface;
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
        ILogger _logger;

        #region 正在组装的包
        /// <summary>
        /// 正在组装的包
        /// </summary>
        Package _creatingPackage;

        /// <summary>
        /// 正在构建中的包当前步骤还需要多少字节
        /// </summary>
        int _creatingNeedLength;

        /// <summary>
        /// 正在构建的包的步骤
        /// </summary>
        PackageCreateStage _creatingStage;
        #endregion

        public PackageFactory(
            ILoggerFactory loggerFactory
            )
        {
            _logger = loggerFactory.CreateLogger<PackageFactory>();

            _creatingStage = PackageCreateStage.Code;
            _creatingNeedLength = 1;
        }

        /// <summary>
        /// 通过不断的压入 buffer ，来完成一个 package 的构建
        /// TODO：暂时把逻辑都写在这个函数来
        /// </summary>
        /// <param name="package"></param>
        /// <param name="buffer"></param>
        /// <param name="offest"></param>
        /// <returns></returns>
        public bool Create(out Package package, byte[] buffer, ref int offest)
        {
            if (_creatingStage == PackageCreateStage.Code)
            {
                _creatingPackage = new Package();

                var fst = buffer[offest++];
            }

            if (_creatingStage == PackageCreateStage.Data && _creatingNeedLength == 0)
            {
                package = _creatingPackage;
                return true;
            }
            else
            {
                package = null;
                return false;
            }
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
