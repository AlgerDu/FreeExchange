using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace D.FreeExchange.Core.Models
{
    /// <summary>
    /// client 同步到 server 端的数据
    /// </summary>
    public class ClientDataEventArgs : EventArgs
    {
        /// <summary>
        /// client 的唯一标识
        /// </summary>
        public Guid ClientUid { get; set; }

        /// <summary>
        /// client 的标签
        /// </summary>
        public string Tag { get; set; }
    }
}
