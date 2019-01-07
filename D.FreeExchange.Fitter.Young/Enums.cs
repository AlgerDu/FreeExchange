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

    /// <summary>
    /// 组装包的阶段，不同的阶段，需要的字节数不同
    /// </summary>
    internal enum PackageCreateStage
    {
        Code,

        Rayload,

        ExRayload,

        Data
    }
}
