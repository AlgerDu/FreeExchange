using D.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace D.FreeExchange
{
    /// <summary>
    /// action 执行器
    /// HACK 暂时没有想清楚，暂时根据需求定义成这种样子
    /// </summary>
    public interface IActionExecutor
    {
        /// <summary>
        /// 执行 aciton
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        IResult InvokeAction(IActionExecuteMessage msg);

        /// <summary>
        /// 序列化请求参数
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        IResult SerializeRequest(IActionExecuteMessage msg);

        /// <summary>
        /// 反序列化返回结果
        /// </summary>
        /// <param name="msg"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        IResult<T> DeserializeResponse<T>(IActionExecuteMessage msg);
    }
}
