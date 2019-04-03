using System;
using System.Collections.Generic;
using System.Text;

namespace D.FreeExchange.Protocol.DP
{
    internal enum PackageState
    {
        Empty,
        ToSend,
        Sending,
        Sended,
        ToPackage,
        Packaged
    }

    public enum FlagCode
    {
        Start,
        Middle,
        End,
        Single
    }

    public enum PackageCode
    {
        Connect = 0,
        Disconnect,
        Heart,

        Clean = 4,
        CleanUp,
        Answer,
        Lost,

        Text = 12,
        ByteDescription,
        Byte
    }
}
