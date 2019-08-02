using System;
using System.Collections.Generic;
using System.Text;

namespace D.FreeExchange.Protocol.DP
{
    /// <summary>
    /// 协议状态更改事件参数
    /// </summary>
    public class ProtocolStateChangedEventArgs : EventArgs
    {
        /// <summary>
        /// 旧状态
        /// </summary>
        public ProtocolState OldState { get; set; }

        /// <summary>
        /// 新状态
        /// </summary>
        public ProtocolState NewState { get; set; }

        /// <summary>
        /// 改变的时间点
        /// </summary>
        public DateTimeOffset Time { get; set; }
    }
}
