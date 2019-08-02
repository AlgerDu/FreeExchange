using System;
using System.Collections.Generic;
using System.Text;

namespace D.FreeExchange.Protocol.DP
{
    public class ProtocolOptionsChangedEventArgs
    {
        /// <summary>
        /// 可选参数
        /// </summary>
        public DProtocolOptions Options { get; set; }

        /// <summary>
        /// 字符串编码
        /// </summary>
        public Encoding Encoding { get; set; }
    }
}
