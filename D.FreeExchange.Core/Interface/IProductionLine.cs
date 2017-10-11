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
    /// 管理 filters ，连接，启动，停止，处理事件上报
    /// </summary>
    public interface IProductionLine : IBasket
    {
        void Run();

        void Close();

        event FitterReportHandler OnReport;
    }
}
