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

    internal class ShareData : IShareData
    {
        DProtocolBuilderOptions _options;

        Dictionary<int, IPackageInfo> _sendPaks;
        Dictionary<int, IPackageInfo> _receviePaks;

        public string Uid { get; private set; }

        public Encoding Encoding { get; private set; }

        public bool BuilderIsRunning { get; set; }

        public ManualResetEvent MRE_ContinueSending { get; private set; }

        public DProtocolBuilderOptions Options
        {
            get => _options;
            set
            {
                _options = value;
            }
        }

        public IReadOnlyDictionary<int, IPackageInfo> SendingPaks => _sendPaks;

        public IReadOnlyDictionary<int, IPackageInfo> ReceivingPaks => _receviePaks;

        public ShareData()
        {
            Encoding = Encoding.ASCII;

            Uid = $"DP[{Guid.NewGuid().ToString()}]";

            _sendPaks = new Dictionary<int, IPackageInfo>();
            _receviePaks = new Dictionary<int, IPackageInfo>();
        }

        private void ResetPakBuffers()
        {
            if (_options != null)
            {
                _sendPaks.Clear();
                _receviePaks.Clear();

                for (var i = 0; i < _options.MaxPackageBuffer; i++)
                {
                    _sendPaks.Add(i, new PackageInfo());
                    _receviePaks.Add(i, new PackageInfo());
                }
            }
        }
    }
}
