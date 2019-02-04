using D.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace D.FreeExchange
{
    /// <summary>
    /// 服务
    /// </summary>
    public interface IExchangeServer
    {
        /// <summary>
        /// 所有连入的客户端代理
        /// </summary>
        IEnumerable<IExchangeClientProxy> ClientProxies { get; }

        /// <summary>
        /// 启动
        /// </summary>
        /// <returns></returns>
        Task<IResult> Run();

        IExchangeClientProxy FindByUid(Guid uid);
    }
}
