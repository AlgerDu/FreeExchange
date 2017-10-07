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

    /// <summary>
    /// context 的状态
    /// </summary>
    public enum ContextState
    {
        /// <summary>
        /// client 端，生成
        /// </summary>
        Create = 101,

        /// <summary>
        /// client 端，发送成功
        /// </summary>
        Sended = 102,

        /// <summary>
        /// server 端，接受成功
        /// </summary>
        Received = 201,

        /// <summary>
        /// server 端，执行成功
        /// </summary>
        Executed = 201
    }
}
