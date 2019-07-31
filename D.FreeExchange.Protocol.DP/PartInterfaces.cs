using D.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace D.FreeExchange.Protocol.DP
{
    internal interface IPackageInfo
    {
        IPackage Package { get; set; }

        PackageState State { get; set; }
    }

    /// <summary>
    /// 基础部分；
    /// 为了降低单个类的代码行数，拆分出来的东西
    /// </summary>
    internal interface IProtocolBase
    {
        /// <summary>
        /// 运行实例的唯一 ID
        /// </summary>
        string Uid { get; }

        Encoding Encoding { get; }

        ProtocolState State { get; }

        DProtocolOptions Options { get; }

        IReadOnlyDictionary<int, IPackageInfo> SendingPaks { get; }

        IReadOnlyDictionary<int, IPackageInfo> ReceivingPaks { get; }

        /// <summary>
        /// 通过回调函数发送包数据
        /// </summary>
        /// <param name="package"></param>
        void SendPackage(IPackage package);
    }

    internal interface IPayloadAnalyser
    {
        /// <summary>
        /// 暂时这样写吧，烦躁
        /// </summary>
        /// <param name="encoding"></param>
        /// <param name="options"></param>
        void UpdateParams(Encoding encoding, DProtocolOptions options);

        IEnumerable<IPackage> Analyse(IProtocolPayload payload);
    }

    /// <summary>
    /// 发送部分
    /// </summary>
    internal interface ISendPart : IDisposable
    {
        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="shareData"></param>
        void Init(IProtocolData shareData);

        /// <summary>
        /// 开始发送数据
        /// </summary>
        void Run();

        /// <summary>
        /// 停止发送数据
        /// </summary>
        void Stop();

        void ReceiveAnswer(int pakIndex);

        IResult SendPayloadPackages(IEnumerable<IPackage> packages);
    }

    internal interface IPackageFactory
    {
        IPackage CreatePackage(byte headBuffer);
    }
}
