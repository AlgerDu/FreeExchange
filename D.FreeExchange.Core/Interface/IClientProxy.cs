using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace D.FreeExchange.Core.Interface
{
    /// <summary>
    /// 服务端的 client 代理
    /// </summary>
    public interface IClientProxy : IFreeExchange
    {
        /// <summary>
        /// client 的标签，没有 set 属性
        /// </summary>
        string Tag { get; }

        /// <summary>
        /// 关闭与客户端的连接
        /// </summary>
        void Close();

        event ConnectedHandler OnConnected;

        event ReconnectedHandler OnReconnected;

        event ClosecHandler OnClosed;
    }
}
