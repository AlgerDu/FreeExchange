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

    internal interface IShareData
    {
        string Uid { get; }

        Encoding Encoding { get; }

        bool BuilderIsRunning { get; set; }

        DProtocolOptions Options { get; set; }

        IReadOnlyDictionary<int, IPackageInfo> SendingPaks { get; }

        IReadOnlyDictionary<int, IPackageInfo> ReceivingPaks { get; }
    }

    internal interface IPayloadAnalyser
    {
        IEnumerable<IPackage> Analyse(IProtocolPayload payload);
    }

    /// <summary>
    /// 发送部分
    /// </summary>
    internal interface ISendPart
    {
        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="shareData"></param>
        void Init(IShareData shareData);

        /// <summary>
        /// 开始发送数据
        /// </summary>
        void Run();

        /// <summary>
        /// 停止发送数据
        /// </summary>
        void Stop();

        /// <summary>
        /// 清理所有的缓存
        /// </summary>
        void Clear();

        [Obsolete]
        void ContinueSending();

        [Obsolete]
        void ReceivedIndexPak(int pakIndex);

        void ReceiveAnswer(int pakIndex);

        IResult SendPayloadPackages(IEnumerable<IPackage> packages);

        void SetSendBufferAction(Action<byte[], int, int> action);
    }

    internal interface IPackageFactory
    {
        IPackage CreatePackage(byte headBuffer);
    }
}
