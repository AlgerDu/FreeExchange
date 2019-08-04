using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace D.FreeExchange.Protocol.DP.Interfaces
{
    /// <summary>
    /// 协议状态修改事件
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public delegate void ProtocolStateChangedEventHandler(object sender, ProtocolStateChangedEventArgs e);

    /// <summary>
    /// 参数更改
    /// </summary>
    /// <param name="options"></param>
    public delegate void ProtocolOptionsChangedEventHandler(object sender, ProtocolOptionsChangedEventArgs e);

    /// <summary>
    /// 协议运行的核心
    /// </summary>
    public interface IProtocolCore
    {
        /// <summary>
        /// 运行时唯一 ID
        /// </summary>
        string Uid { get; }

        /// <summary>
        /// 运行时的状态
        /// </summary>
        ProtocolState State { get; }

        /// <summary>
        /// 状态改变事件
        /// </summary>
        event ProtocolStateChangedEventHandler StateChanged;

        /// <summary>
        /// 参数改变时间
        /// </summary>
        event ProtocolOptionsChangedEventHandler OptionsChanged;

        /// <summary>
        /// 更新状态，需要符合状态的轮转，
        /// 否则将状态置为停止后，抛出异常
        /// </summary>
        /// <param name="newState"></param>
        void ChangeState(ProtocolState newState);

        /// <summary>
        /// 刷新可选配置参数
        /// </summary>
        /// <param name="options"></param>
        void RefreshOptons(DProtocolOptions options);

        /// <summary>
        /// 发送包的 buffer 到对面
        /// </summary>
        /// <param name="pak"></param>
        /// <returns></returns>
        Task SendPackage(IPackage pak);

        /// <summary>
        /// 处理合并出来的数据
        /// </summary>
        /// <param name="payload"></param>
        /// <returns></returns>
        Task DealProtocolPayload(IProtocolPayload payload);
    }
}
