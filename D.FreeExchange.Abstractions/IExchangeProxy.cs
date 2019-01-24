using D.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace D.FreeExchange
{
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

        /// <summary>
        /// 异步发送数据（泛型返回值）
        /// </summary>
        /// <typeparam name="T">返回数据的类型</typeparam>
        /// <param name="url">URL</param>
        /// <param name="param">数据</param>
        /// <param name="sendTimeout">发送超时时间</param>
        /// <param name="receiveTimeout">接受超时时间</param>
        /// <returns></returns>
        //Task<IResult<T>> SendAsync<T>(
        //    string url
        //    , object[] param
        //    , TimeSpan sendTimeout
        //    , TimeSpan receiveTimeout) where T : class, new();
    }

    public static class IExchangeProxy_Extensions
    {
        //public static T Send<T>(
        //    this IExchangeProxy proxy
        //    , string url, object[] param, TimeSpan sendTimeout, TimeSpan receiveTimeout
        //    ) where T : IResult, new()
        //{
        //    var task = proxy.SendAsync<T>(url, param, sendTimeout, receiveTimeout);
        //    task.Wait();
        //    return task.Result;
        //}

        //public static IResult<T> SendAsync<T>(
        //    this IExchangeProxy proxy
        //    , string url, object[] param, TimeSpan sendTimeout, TimeSpan receiveTimeout
        //    ) where T : class, new()
        //{
        //    var task = proxy.SendAsync<IResult<T>>(url, param, sendTimeout, receiveTimeout);
        //    task.Wait();
        //    return task.Result;
        //}
    }
}
