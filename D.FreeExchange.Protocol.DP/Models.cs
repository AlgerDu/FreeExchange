using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace D.FreeExchange.Protocol.DP
{
    internal class PackageInfo : IPackageInfo
    {
        PackageState _state;

        public IPackage Package { get; set; }

        public PackageState State
        {
            get => _state;
            set
            {
                _state = value;

                if (_state == PackageState.Empty)
                {
                    Package = null;
                }
            }
        }

        public PackageInfo()
        {
            _state = PackageState.Empty;
        }
    }
}
