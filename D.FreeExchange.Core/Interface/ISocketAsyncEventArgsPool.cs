using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace D.FreeExchange.Core.Interface
{
    /// <summary>
    /// SocketAsyncEventArgs pool
    /// 后面要考虑将此接口转移到其它地方
    /// </summary>
    public interface ISocketAsyncEventArgsPool
    {
        /// <summary>
        /// 获取一个可用的 SocketAsyncEventArgs
        /// </summary>
        /// <returns></returns>
        SocketAsyncEventArgs Pop();

        /// <summary>
        /// 将一个不再使用的 SocketAsyncEventArgs 重新放回 pool 中
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        bool Push(SocketAsyncEventArgs arg);
    }
}
