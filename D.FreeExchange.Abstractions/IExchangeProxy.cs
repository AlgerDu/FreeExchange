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
        Guid Uid { get; }

        bool Online { get; }

        Task<IResult> SendAsync(string url, object[] param, TimeSpan sendTimeout, TimeSpan receiveTimeout);

        Task<IResult<T>> SendAsync<T>(string url, object[] param, TimeSpan sendTimeout, TimeSpan receiveTimeout) where T : class, new();
    }

    public static class IExchangeProxy_Extensions
    {
        public static IResult Send(
            this IExchangeProxy proxy
            , string url, object[] param, TimeSpan sendTimeout, TimeSpan receiveTimeout
            )
        {
            var task = proxy.SendAsync(url, param, sendTimeout, receiveTimeout);
            task.Wait();
            return task.Result;
        }

        public static IResult<T> Send<T>(
            this IExchangeProxy proxy
            , string url, object[] param, TimeSpan sendTimeout, TimeSpan receiveTimeout
            ) where T : class, new()
        {
            var task = proxy.SendAsync<T>(url, param, sendTimeout, receiveTimeout);
            task.Wait();
            return task.Result;
        }
    }
}
