using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace D.FreeExchange.Core.Interface
{
    /// <summary>
    /// 装配流水线
    /// </summary>
    public interface IProductionLine
    {
        /// <summary>
        /// 给生产线添加 socket 原料
        /// 可以被重复调用，用于重连
        /// </summary>
        /// <param name="socket"></param>
        void AddMaterial(Socket socket);

        void Run();

        void Close();
    }
}
