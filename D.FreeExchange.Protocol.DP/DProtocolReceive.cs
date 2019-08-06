using D.FreeExchange.Protocol.DP;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace D.FreeExchange.Protocol.DP
{
    /// <summary>
    /// 一些基础公用的数据部分
    /// </summary>
    public class DProtocoloReceive : IProtocolReceive
    {
        ILogger _logger;
        IProtocolCore _core;
        bool _isReceiving;

        Dictionary<int, PackageCacheItem> _receivingPaks;

        int _maxReceiveIndex;
        int _maxPakBuffer;
        int _lastCleanIndex;

        Encoding _encoding;

        public DProtocoloReceive(
            ILogger logger
            , IProtocolCore core
            )
        {
            _logger = logger;
            _core = core;

            _core.OptionsChanged += new ProtocolOptionsChangedEventHandler(OnOptionsChanged);
            _core.StateChanged += new ProtocolStateChangedEventHandler(OnStateChanged);

            _isReceiving = false;
        }

        public override string ToString()
        {
            return $"{_core} receive";
        }

        #region IProtocolReceive 接口实现

        public void DealIndexPackage(IPackage package)
        {
            if (!_isReceiving)
                return;

            switch (package.Code)
            {
                case PackageCode.Clean:
                    DealClean(package);
                    break;

                case PackageCode.Text:
                case PackageCode.ByteDescription:
                case PackageCode.Byte:
                    DealData(package);
                    break;

                default:
                    _logger.LogWarning($"{this} 不能处理 {package.Code} 类型的包数据");
                    break;
            }
        }

        #endregion

        private void OnStateChanged(object sender, ProtocolStateChangedEventArgs e)
        {
            if (e.NewState == ProtocolState.Closing || e.NewState == ProtocolState.Stop)
            {
                //停止并且清理
                StopReceiving();
                Clear();
            }
            else if (e.OldState == ProtocolState.Connectting && e.NewState == ProtocolState.Online)
            {
                //启动发送
                StopReceiving();
            }
            else if (e.OldState == ProtocolState.Online && e.NewState == ProtocolState.Offline)
            {
                //停止发送
                StopReceiving();
            }
        }

        private void OnOptionsChanged(object sender, ProtocolOptionsChangedEventArgs e)
        {
            //清理之后，
            //重新设置相关缓存的大小
            Clear();

            _maxPakBuffer = e.Options.MaxPackageBuffer;

            _maxReceiveIndex = _maxPakBuffer * 4;

            _receivingPaks = new Dictionary<int, PackageCacheItem>(_maxReceiveIndex);

            _encoding = e.Encoding;

            for (var i = 0; i < _maxReceiveIndex; i++)
            {
                _receivingPaks.Add(i, new PackageCacheItem());
            }
        }

        /// <summary>
        /// 开始接收
        /// </summary>
        private void StartReceiving()
        {
            lock (this)
            {
                _isReceiving = true;
            }
        }

        /// <summary>
        /// 停止接收
        /// </summary>
        private void StopReceiving()
        {
            lock (this)
            {
                _isReceiving = false;
            }
        }

        /// <summary>
        /// 清理缓存中的东西
        /// </summary>
        private void Clear()
        {
            lock (this)
            {
                _receivingPaks?.Clear();

                _lastCleanIndex = -1;
            }
        }

        /// <summary>
        /// 处理清理包
        /// </summary>
        /// <param name="package"></param>
        private void DealClean(IPackage package)
        {
            var pak = package as PackageWithIndex;

            lock (this)
            {
                if (_lastCleanIndex != pak.Index)
                {
                    _lastCleanIndex = pak.Index;

                    var offset = 1;

                    for (var i = pak.Index
                        ; offset < _maxPakBuffer
                        ; i = (i + _maxReceiveIndex + 1) % _maxReceiveIndex)
                    {
                        _receivingPaks[i].State = PackageState.Empty;
                        _receivingPaks[i].Package = null;
                    }
                }
            }

            //清理完之后才发送清理的回复包
            SendAnswerPak(pak.Index);
        }

        /// <summary>
        /// 发送回复包
        /// </summary>
        /// <param name="index"></param>
        private async void SendAnswerPak(int index)
        {
            await Task.Run(() =>
            {
                var pak = new PackageWithIndex(PackageCode.Answer);
                pak.Index = index;

                _core.SendPackage(pak);
            });
        }

        /// <summary>
        /// 处理数据包
        /// </summary>
        /// <param name="package"></param>
        private void DealData(IPackage package)
        {
            var payloadPak = package as PackageWithPayload;
            var index = payloadPak.Index;

            SendAnswerPak(index);

            lock (this)
            {
                if (_receivingPaks[index].State != PackageState.Empty)
                {
                    // 收到了重复的数据包，不做处理
                    return;
                }
                else
                {
                    _receivingPaks[index].State = PackageState.ToPackage;
                    _receivingPaks[index].Package = payloadPak;
                }
            }

            if (package.Flag == FlagCode.End || package.Flag == FlagCode.Single)
            {
                TryPackPackageTask(index);
            }
        }

        private async void TryPackPackageTask(int finIndex)
        {
            await Task.Run(() =>
            {
                var info = _receivingPaks[finIndex];

                if (info.Package.Flag == FlagCode.Single)
                {
                    PackToPayloadAndDeal(finIndex);
                    return;
                }

                var can = false;
                var startIndx = (finIndex + _maxReceiveIndex - 1) % _maxReceiveIndex;

                var goon = true;

                do
                {
                    if (_receivingPaks[startIndx].State == PackageState.Empty)
                    {
                        goon = false;
                    }
                    else if (_receivingPaks[startIndx].Package.Flag == FlagCode.Start)
                    {
                        goon = false;
                        can = true;
                    }
                    else
                    {
                        startIndx = (startIndx + _maxReceiveIndex - 1) % _maxReceiveIndex;
                    }
                } while (goon);

                if (can)
                {
                    PackToPayloadAndDeal(startIndx);
                }
            });
        }

        private void PackToPayloadAndDeal(int index)
        {
            var pak = _receivingPaks[index].Package as PackageWithPayload;

            var dataPakCount = pak.PayloadLength;
            var buffer = new List<byte>();

            var lastPakCode = PackageCode.Text;

            var payload = new ProtocolPayload()
            {
                Bytes = new ByteDescription[0]
            };

            do
            {
                pak = _receivingPaks[index].Package as PackageWithPayload;

                if (pak.Code == lastPakCode)
                {
                    buffer.AddRange(pak.Payload);
                }

                if (pak.Flag == FlagCode.End
                    || pak.Flag == FlagCode.Single
                    || pak.Code != lastPakCode)
                {
                    switch (lastPakCode)
                    {
                        case PackageCode.Text:
                            {
                                payload.Text = _encoding.GetString(buffer.ToArray());
                            }
                            break;
                    }
                }

                lastPakCode = pak.Code;
                index = (index + 1) % _maxReceiveIndex;
            }
            while (pak.Flag != FlagCode.End && pak.Flag != FlagCode.Single);

            _core.DealProtocolPayload(payload);
        }
    }
}
