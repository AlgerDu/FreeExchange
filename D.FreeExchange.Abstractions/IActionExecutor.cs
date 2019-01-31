using System;
using System.Collections.Generic;
using System.Text;

namespace D.FreeExchange
{
    /// <summary>
    /// action 执行器
    /// </summary>
    public interface IActionExecutor
    {
        /// <summary>
        /// 执行 aciton
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        object InvokeAction(IExchangeMessage msg);
    }
}
