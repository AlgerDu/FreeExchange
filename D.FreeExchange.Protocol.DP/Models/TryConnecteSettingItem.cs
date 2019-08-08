using System;
using System.Collections.Generic;
using System.Text;

namespace D.FreeExchange.Protocol.DP
{
    /// <summary>
    /// 尝试连接的配置项
    /// </summary>
    public class TryConnecteSettingItem
    {
        /// <summary>
        /// 尝试连接的时间间隔
        /// </summary>
        public TimeSpan Interval { get; set; }

        /// <summary>
        /// 尝试次数
        /// </summary>
        public int TryCount { get; set; }
    }
}
