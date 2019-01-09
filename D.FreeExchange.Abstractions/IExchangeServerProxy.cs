using D.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace D.FreeExchange
{
    /// <summary>
    /// 服务代理
    /// </summary>
    public interface IExchangeServerProxy : IExchangeProxy
    {
        /// <summary>
        /// 连接服务端
        /// </summary>
        /// <returns></returns>
        Task<IResult> Connect();

        /// <summary>
        /// 更新服务器的地址
        /// </summary>
        /// <param name="newAddress"></param>
        /// <returns></returns>
        IResult UpdateAddress(string newAddress);
    }
}
