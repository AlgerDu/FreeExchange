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
        public bool Fin { get; set; }

        public PackageCode Code { get; set; }

        public Int32 RayloadLen { get; set; }

        public Int32 ExtRayloadLen { get; set; }

        public Int32 RealRayloadLen
        {
            get => RayloadLen >= 126 ? ExtRayloadLen : RayloadLen;
        }
    }
}
