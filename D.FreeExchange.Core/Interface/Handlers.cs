using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace D.FreeExchange.Core.Interface
{
    /// <summary>
    /// 处理数据交换的委托
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="context"></param>
    public delegate void OnExchangeHandler(IFreeExchange sender, IContext context);

    /// <summary>
    /// 连接成功
    /// </summary>
    /// <param name="sender"></param>
    public delegate void ConnectedHandler(object sender);

    /// <summary>
    /// 重连成功
    /// </summary>
    /// <param name="sender"></param>
    public delegate void ReconnectedHandler(object sender);

    /// <summary>
    /// 已经关闭
    /// 请不要在处理此事件的过程中调用任何 IClent 上任何有关清理的函数
    /// </summary>
    /// <param name="sender"></param>
    public delegate void ClosecHandler(object sender);

    /// <summary>
    /// fitter 上报处理，现在能想到的就是 close
    /// </summary>
    /// <param name="sender"></param>
    /// <param name=""></param>
    public delegate void FitterReportHandler(IFitter sender, FitterReportEvent e);
}
