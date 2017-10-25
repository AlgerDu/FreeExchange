using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace D.FreeExchange.Fitter.Young
{
    /// <summary>
    /// 包类型
    /// </summary>
    internal enum PackageCode
    {
        /// <summary>
        /// 心跳
        /// </summary>
        Ping,

        /// <summary>
        /// 关闭
        /// </summary>
        Close,

        /// <summary>
        /// 文本
        /// </summary>
        Txt,

        /// <summary>
        /// 字节包描述
        /// </summary>
        ByteDescript,

        /// <summary>
        /// 字节
        /// </summary>
        Byte
    }
}
