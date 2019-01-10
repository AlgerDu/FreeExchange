using D.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace D.FreeExchange
{
    /// <summary>
    /// buffer 搬运工
    /// </summary>
    public interface ITransporter
    {
        /// <summary>
        /// 发送 buffer 到远程
        /// </summary>
        /// <param name="buffer">字节数组</param>
        /// <param name="index">起始位</param>
        /// <param name="length">发送长度</param>
        /// <returns>是否成功</returns>
        Task<IResult> SendAsync(byte[] buffer, int index, int length);

        /// <summary>
        /// 设置接受到 buffer 的 action
        /// </summary>
        /// <param name="action">buffer 起始位 长度</param>
        void SetReceiveAction(Action<byte[], int, int> action);

        /// <summary>
        /// 连接
        /// </summary>
        /// <returns>可能超时等等</returns>
        Task<IResult> Connect();

        /// <summary>
        /// 关闭连接
        /// </summary>
        /// <returns></returns>
        Task<IResult> Close();
    }
}
