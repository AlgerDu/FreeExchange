using D.FreeExchange.Core.Models;
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

        /// <summary>
        /// 代替 rProxy 与其 client 通讯，主要用于 client 重连，重用 server 端的缓存数据
        /// 一般情况下只需要 IServer 进行调用
        /// </summary>
        /// <param name="proxy"></param>
        void Replease(IClientProxy rProxy);

        /// <summary>
        /// client 同步数据到 server，重连时，IServer 先调用 IClientProxy.Replease，在调用此函数
        /// </summary>
        event EventHandler<ClientDataEventArgs> SyncingData;
    }
}
