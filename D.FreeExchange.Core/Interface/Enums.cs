using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace D.FreeExchange.Core.Interface
{
    /// <summary>
    /// 给 fitter 的命令类型
    /// </summary>
    public enum FitterCommand
    {
        Run,
        Close
    }

    public enum FitterReportEvent
    {
        Close
    }
}
