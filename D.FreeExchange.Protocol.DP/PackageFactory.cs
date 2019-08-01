using System;
using System.Collections.Generic;
using System.Text;

namespace D.FreeExchange.Protocol.DP
{
    internal class PackageFactory : IPackageFactory
    {
        public IPackage CreatePackage(byte headBuffer)
        {
            var header = new PackageHeader(headBuffer);

            switch (header.Code)
            {
                case PackageCode.Connect:
                    return new ConnectPackage(header);

                case PackageCode.ConnectOK:
                    return new ConnectOkPackage(header);

                case PackageCode.Heart:
                    return new HeartPackage();

                case PackageCode.Disconnect:
                    return new DisconnectPackage();

                case PackageCode.Clean:
                case PackageCode.CleanUp:
                case PackageCode.Answer:
                case PackageCode.Lost:
                    return new PackageWithIndex(header);

                case PackageCode.Text:
                case PackageCode.ByteDescription:
                case PackageCode.Byte:
                    return new PackageWithPayload(header);

                default:
                    throw new Exception($"不支持的 package code {header.Code}");
            }
        }
    }
}
