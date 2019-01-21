using D.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace D.FreeExchange
{
    /// <summary>
    /// 协议建造器
    /// </summary>
    public interface IProtocolBuilder
    {
        /// <summary>
        /// 发送数据
        /// </summary>
        /// <param name="payload"></param>
        /// <returns></returns>
        Task<IResult> SendAsync(IProtocolPayload payload);

        /// <summary>
        /// 收到数据的回调
        /// </summary>
        /// <param name="action"></param>
        void SetPayloadReceiveAction(Action<IProtocolPayload> action);

        /// <summary>
        /// 设置控制的接受
        /// </summary>
        /// <param name="action"></param>
        void SetControlReceiveAction(Action<int> action);

        Task<IResult> Run();

        Task<IResult> Stop();
    }
}
