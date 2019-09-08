using D.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace D.FreeExchange
{
    /// <summary>
    /// ExchangeProxy 恢复在线
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public delegate void ExchangeProxyBackOnlineEventHandler(object sender, ExchangeProxyEventArgs e);

    /// <summary>
    /// ExchangeProxy 离线
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public delegate void ExchangeProxyOfflineEventHandler(object sender, ExchangeProxyEventArgs e);

    /// <summary>
    /// ExchangeProxy 连接成功
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public delegate void ExchangeProxyConnectedEventHandler(object sender, ExchangeProxyEventArgs e);

    /// <summary>
    /// ExchangeProxy 即将关闭
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public delegate void ExchangeProxyClosingEventHandler(object sender, ExchangeProxyEventArgs e);

    /// <summary>
    /// 交换代理
    /// </summary>
    public interface IExchangeProxy
    {
        /// <summary>
        /// 代理的 UID
        /// </summary>
        Guid Uid { get; }

        /// <summary>
        /// 是否在线
        /// </summary>
        bool Online { get; }

        /// <summary>
        /// 地址
        /// </summary>
        string Address { get; }

        event ExchangeProxyBackOnlineEventHandler BackOnline;
        event ExchangeProxyOfflineEventHandler Offline;
        event ExchangeProxyConnectedEventHandler Connected;
        event ExchangeProxyClosingEventHandler Closing;

        /// <summary>
        /// 主动断开与远程的连接
        /// </summary>
        /// <returns></returns>
        Task<IResult> Disconnect();

        /// <summary>
        /// 异步发送数据
        /// </summary>
        /// <param name="url">URL</param>
        /// <param name="param">数据</param>
        /// <param name="sendTimeout">发送超时时间</param>
        /// <param name="receiveTimeout">接受超时时间</param>
        /// <returns>无特定类型的返回值</returns>
        Task<T> SendAsync<T>(IExchangeMessage msg) where T : IResult, new();
    }
}