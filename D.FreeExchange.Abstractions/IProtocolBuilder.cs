﻿using D.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace D.FreeExchange
{
    /// <summary>
    /// 协议建造器
    /// 1、将 IProtocolPayload 按照协议转换为 buffer 数组
    /// 2、将 buffer 数组按照协议重新组装回 IProtocolPayload
    /// </summary>
    public interface IProtocolBuilder
    {
        /// <summary>
        /// 启动运行
        /// </summary>
        /// <param name="mode">以那种模式运行</param>
        /// <returns></returns>
        Task<IResult> Run(ProtocolBuilderRunningMode mode);

        Task<IResult> Stop();

        /// <summary>
        /// 向 IProtocolBuilder 推送需要解析的 buffer 数据
        /// </summary>
        /// <param name="buffer">byte 数组</param>
        /// <param name="offset">偏移量</param>
        /// <param name="length"></param>
        /// <returns></returns>
        IResult PushBuffer(byte[] buffer, int offset, int length);

        /// <summary>
        /// 向 IProtocolBuilder 推送需要解析的 payload 数据
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
        void SetReceivedControlAction(Action<int> action);

        /// <summary>
        /// 设置当需要发送 buffer 数据时的回调函数
        /// </summary>
        /// <param name="action"></param>
        void SetSendBufferAction(Action<byte[], int, int> action);
    }
}
