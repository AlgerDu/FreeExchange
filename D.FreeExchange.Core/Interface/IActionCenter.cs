using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace D.FreeExchange.Core.Interface
{
    /// <summary>
    /// 接受到请求之后执行请求
    /// 解析 url 和 params ，之后执行 action
    /// 辅助接口，不是必需的
    /// </summary>
    public interface IActionCenter
    {
        /// <summary>
        /// 执行请求，类 mvc
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        bool Execute(IContext context);
    }
}
