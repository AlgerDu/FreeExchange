using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace D.FreeExchange.Fitter.Young
{
    /// <summary>
    /// YoungFitter 包文件
    /// </summary>
    internal class Package
    {
        /// <summary>
        /// 是否结束包
        /// </summary>
        public bool Fin { get; set; }

        /// <summary>
        /// 包类型码
        /// </summary>
        public PackageCode Code { get; set; }

        /// <summary>
        /// 携带的数据长度
        /// </summary>
        public Int32 RayloadLen { get; set; }

        /// <summary>
        /// 扩展的携带数据长度
        /// </summary>
        public Int32 ExtRayloadLen { get; set; }

        /// <summary>
        /// 包携带的真实的数据长度
        /// </summary>
        public Int32 RealRayloadLen
        {
            get => RayloadLen >= 126 ? ExtRayloadLen : RayloadLen;
        }

        /// <summary>
        /// 携带的数据
        /// </summary>
        public byte[] Data { get; set; }
    }
}
