using System;
using System.Collections.Generic;
using System.Text;

namespace D.FreeExchange
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
}
