﻿using D.Utils;
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
        Encoding Encoding { get; }

        bool BuilderIsRunning { get; set; }

        ManualResetEvent MRE_ContinueSending { get; }

        DProtocolBuilderOptions Options { get; set; }

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
        IResult SendCmdPakcage(IPackage package);

        IResult SendPayloadPackages(IEnumerable<IPackage> packages);

        void SetSendBufferAction();
    }

    internal interface IPackageFactory
    {
        IPackage CreatePackage(byte headBuffer);
    }
}