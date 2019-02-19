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
        /// 更新服务代理的 transporter
        /// </summary>
        /// <param name="transporter"></param>
        /// <returns></returns>
        IResult UpdateTransporter(ITransporter transporter);
    }
}
