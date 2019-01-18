using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace D.FreeExchange
{
    /// <summary>
    /// 自定义 DProtocol 的一些可选参数
    /// </summary>
    public class DProtocolBuilderOptions
    {
        /// <summary>
        /// 数据包中数据的最大长度；
        /// 最大值为 65536
        /// </summary>
        [DefaultValue(65536)]
        public int MaxPayloadDataLength { get; set; }

        /// <summary>
        /// 数据包的带发送缓存数量；
        /// </summary>
        [DefaultValue(2048)]
        public int MaxPackageBuffer { get; set; }

        /// <summary>
        /// 心跳时间间隔，仅客户端有效
        /// </summary>
        [DefaultValue(10)]
        public int HeartInterval { get; set; }
    }
}
