using D.FreeExchange.Protocol.DP;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace D.FreeExchange
{
    /// <summary>
    /// 一些基础公用的数据部分
    /// </summary>
    public partial class DProtocol
    {
        /// <summary>
        /// 运行实例的唯一 ID
        /// </summary>
        readonly string _uid;

        readonly Encoding _encoding;

        ProtocolState _state;

        DProtocolOptions _options;

        IDictionary<int, IPackageInfo> _sendingPaks;

        IDictionary<int, IPackageInfo> _receivingPaks;

        int _receiveMaxIndex;
        int _lastCleanIndex;

        ProtocolState State
        {
            set
            {
                _logger.LogTrace($"{this} state {_state} => {value}");

                _state = value;
            }
        }

        /// <summary>
        /// 通过重新设置 options 来调整内部缓存数据的大小
        /// </summary>
        /// <param name="options"></param>
        private void ResetOptions(DProtocolOptions options)
        {
            _sendingPaks?.Clear();
            _receivingPaks?.Clear();

            _options = options;

            _payloadAnalyser.UpdateParams(_encoding, _options);

            _receiveMaxIndex = _options.MaxPackageBuffer * 4;

            _sendingPaks = new Dictionary<int, IPackageInfo>();
            _receivingPaks = new Dictionary<int, IPackageInfo>();

            for (var i = 0; i < _options.MaxPackageBuffer; i++)
            {
                _sendingPaks.Add(i, new PackageInfo());
                _receivingPaks.Add(i, new PackageInfo());
            }
        }
    }
}
