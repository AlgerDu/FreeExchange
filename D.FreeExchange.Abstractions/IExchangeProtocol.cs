﻿using D.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace D.FreeExchange
{
    /// <summary>
    /// 交换协议
    /// </summary>
    public interface IExchangeProtocol
    {
        /// <summary>
        /// 启动运行
        /// </summary>
        /// <returns></returns>
        Task<IResult> Run();

        /// <summary>
        /// 停止运行，清理所有的缓存
        /// </summary>
        /// <returns></returns>
        Task<IResult> Stop();

        /// <summary>
        /// 向 IExchangeProtocol 推送需要解析的 buffer 数据
        /// </summary>
        /// <param name="buffer">byte 数组</param>
        /// <param name="offset">偏移量</param>
        /// <param name="length"></param>
        /// <returns></returns>
        IResult PushBuffer(byte[] buffer, int offset, int length);

        /// <summary>
        /// 向 IProtocolBuilder 推送需要解析的 payload 数据
        /// TODO 这里到底需不需要异步，还是上面的接口也改成异步
        /// </summary>
        /// <param name="payload"></param>
        /// <returns></returns>
        Task<IResult> PushPayload(IProtocolPayload payload);

        /// <summary>
        /// 设置接收到 IProtocolPayload 的回调函数
        /// </summary>
        /// <param name="action"></param>
        void SetReceivedPayloadAction(Action<IProtocolPayload> action);

        /// <summary>
        /// 设置接收到控制命令的回调函数
        /// </summary>
        /// <param name="action"></param>
        void SetReceivedCmdAction(Action<int, DateTimeOffset> action);

        /// <summary>
        /// 设置当需要发送 buffer 数据时的回调函数
        /// </summary>
        /// <param name="action"></param>
        void SetSendBufferAction(Action<byte[], int, int> action);
    }
}
