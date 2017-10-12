using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace D.FreeExchange.Core.Interface
{
    /// <summary>
    /// client 接口
    /// </summary>
    /// <typeparam name="T">自定义的数据类型</typeparam>
    public interface IClient : IFreeExchange
    {
        /// <summary>
        /// 客户端的自定义标签，会同步到 server 端的 proxy
        /// 什么时候同步：
        /// 1、连接成功之后会自动同步一次；
        /// 2、发送主动的同步命令
        /// </summary>
        string Tag { get; set; }

        /// <summary>
        /// 连接 server
        /// </summary>
        /// <param name="address">server 地址</param>
        void Connect(string address);

        /// <summary>
        /// 关闭
        /// </summary>
        void Close();

        /// <summary>
        /// 主动同步一些数据，如 tag
        /// </summary>
        void SyncData();

        /// <summary>
        /// 成功连接服务器
        /// </summary>
        event EventHandler Connected;

        /// <summary>
        /// 与服务器重连成功
        /// </summary>
        event EventHandler Reconnected;

        /// <summary>
        /// 已经关闭
        /// </summary>
        event EventHandler Closed;
    }

    /// <summary>
    /// 带有 用户自定义数据 client 接口
    /// </summary>
    /// <typeparam name="T">自定义的数据类型</typeparam>
    public interface IClient<T> : IClient
        where T : class, new()
    {
        /// <summary>
        /// 自定义的一些用户数据
        /// </summary>
        T CustomerData { get; set; }
    }
}
