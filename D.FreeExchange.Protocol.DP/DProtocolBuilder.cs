using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using D.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace D.FreeExchange
{
    public class DProtocolBuilder : IProtocolBuilder
    {
        readonly Encoding _encoding = Encoding.ASCII;

        ILogger _logger;
        DProtocolBuilderOptions _options;
        bool _builderRunning;

        Action<IProtocolPayload> _receivedPayloadAction;
        Action<int> _receivedControlAction;
        Action<byte[], int, int> _sendBufferAction;

        public DProtocolBuilder(
            ILogger<DProtocolBuilder> logger
            , IOptions<DProtocolBuilderOptions> options
            )
        {
            _logger = logger;

            _options = options.Value;

            _builderRunning = false;

            InitSend();
            InitReceive();
        }

        #region IProtocolBuilder 实现
        public Task<IResult> Run()
        {
            return Task.Run<IResult>(() =>
            {
                lock (this)
                {
                    _builderRunning = true;

                    Task.Run(() => DistributeIndexTask());
                }

                return Result.CreateSuccess();
            });
        }

        public Task<IResult> Stop()
        {
            return Task.Run<IResult>(() =>
            {
                lock (this)
                {
                    _builderRunning = false;

                    _morePackagesToDistrubuteMre.Set();
                }

                return Result.CreateSuccess();
            });
        }

        public void SetReceivedPayloadAction(Action<IProtocolPayload> action)
        {
            _receivedPayloadAction = action;
        }

        public void SetReceivedControlAction(Action<int> action)
        {
            _receivedControlAction = action;
        }

        public void SetSendBufferAction(Action<byte[], int, int> action)
        {
            _sendBufferAction = action;
        }

        public IResult PushBuffer(byte[] buffer, int offset, int length)
        {
            TransporterReceivedBuffer(buffer, offset, length);

            return Result.CreateSuccess();
        }

        public Task<IResult> PushPayload(IProtocolPayload payload)
        {
            return Task.Run(() => AnalysePayloadToPackage(payload));
        }
        #endregion

        #region 发送，暂时先都写在这里，写完在考虑封装整理

        object _sendLock = new object();
        bool _sendTaskIsRunning = false;

        int _maxSendIndex;
        int _currSendIndex;

        ManualResetEvent _morePackagesToDistrubuteMre;
        ManualResetEvent _continueSendingMre;

        /// <summary>
        /// 等待分配 index 的 packages
        /// </summary>
        Queue<Package> _toDistributeIndexPackages;

        /// <summary>
        /// 已经分配好 index 等待发送的 packages
        /// </summary>
        Dictionary<int, Package> _toSendPackages;
        Dictionary<int, PackageState> _sentMark;

        /// <summary>
        /// 初始化 send 相关的一些数据
        /// </summary>
        private void InitSend()
        {
            _toDistributeIndexPackages = new Queue<Package>(_options.MaxPackageBuffer);
            _toSendPackages = new Dictionary<int, Package>(_options.MaxPackageBuffer * 4);
            _sentMark = new Dictionary<int, PackageState>(_options.MaxPackageBuffer * 4);

            _morePackagesToDistrubuteMre = new ManualResetEvent(false);
            _continueSendingMre = new ManualResetEvent(false);

            _maxSendIndex = _options.MaxPackageBuffer;
            _currSendIndex = 0;

            for (var i = 0; i < _options.MaxPackageBuffer * 4; i++)
            {
                _sentMark.Add(i, PackageState.Empty);
                _toSendPackages.Add(i, null);
            }
        }

        private IResult AnalysePayloadToPackage(IProtocolPayload payload)
        {
            List<Package> packages = new List<Package>();

            var textBuffer = _encoding.GetBytes(payload.Text);
            BufferToPackages(packages, textBuffer, PackageCode.Text);

            if (packages.Count == 1)
            {
                packages[0].Flag = FlagCode.Single;
            }
            else
            {
                packages[0].Flag = FlagCode.Start;
                packages[packages.Count - 1].Flag = FlagCode.End;
            }

            lock (_sendLock)
            {
                if (_toDistributeIndexPackages.Count + packages.Count > _options.MaxPackageBuffer)
                {
                    return Result.Create((int)ExchangeCode.SentBufferFull, "缓存已满");
                }
                else
                {
                    foreach (var package in packages)
                    {
                        _toDistributeIndexPackages.Enqueue(package);
                    }

                    _morePackagesToDistrubuteMre.Set();

                    return Result.CreateSuccess();
                }
            }
        }

        private void BufferToPackages(List<Package> packages, byte[] buffer, PackageCode code)
        {
            var offeset = 0;

            do
            {
                var length = buffer.Length - offeset < _options.MaxPayloadDataLength
                    ? buffer.Length - offeset
                    : _options.MaxPayloadDataLength;

                var package = new Package(length)
                {
                    Code = code
                };

                Array.Copy(buffer, offeset, package.Data, 0, length);

                offeset += length;

                packages.Add(package);

            } while (offeset < buffer.Length);
        }

        private void DistributeIndexTask()
        {
            while (_builderRunning)
            {
                Package toDistributeIndexPackage = null;

                lock (_sendLock)
                {
                    if (_toDistributeIndexPackages.Count > 0)
                    {
                        toDistributeIndexPackage = _toDistributeIndexPackages.Dequeue();
                    }
                }

                if (toDistributeIndexPackage == null)
                {
                    _morePackagesToDistrubuteMre.WaitOne();
                }
                else
                {
                    toDistributeIndexPackage.Index = _currSendIndex;

                    SendPackage(toDistributeIndexPackage);

                    _currSendIndex = (_currSendIndex + 1) % _maxSendIndex;
                }

                if ((_currSendIndex + 1) % _options.MaxPackageBuffer == 0)
                {
                    CleanAndRepeat();

                    _continueSendingMre.WaitOne();
                }
            }
        }

        private Task CleanAndRepeat()
        {
            return Task.Run(() =>
            {
                var cleanCount = 0;
                var toCleanIndex = (_currSendIndex + 1) % _maxSendIndex;

                SendCleanPak(toCleanIndex);

                do
                {
                    _toSendPackages[toCleanIndex] = null;
                    _sentMark[toCleanIndex] = PackageState.Empty;

                    toCleanIndex = (toCleanIndex + 1) % _maxSendIndex;
                    cleanCount++;
                } while (cleanCount < _options.MaxPackageBuffer);

                var repeatOffest = 0;
                var repeatStart = (_currSendIndex + _options.MaxPackageBuffer) % _maxSendIndex;

                do
                {
                    var toRepeatIndex = repeatStart + repeatOffest;
                    var need = false;

                    lock (_sendLock)
                    {
                        need = _sentMark[toRepeatIndex] == PackageState.Sending
                            || _sentMark[toRepeatIndex] == PackageState.ToSend;
                    }

                    if (need)
                    {
                        SendPackage(toRepeatIndex);
                    }

                    repeatOffest++;

                } while (repeatOffest < _options.MaxPackageBuffer * 2);
            });
        }

        private Task SendPackage(Package package)
        {
            return Task.Run(() =>
            {
                if (_sendBufferAction != null)
                {
                    var buffer = package.ToBuffer();
                    _sendBufferAction(buffer, 0, buffer.Length);

                    lock (_sendLock)
                    {
                        _sentMark[package.Index] = PackageState.Sending;
                    }
                }
                else
                {
                    lock (_sendLock)
                    {
                        _sentMark[package.Index] = PackageState.ToSend;
                    }
                }
            });
        }

        private void SendPackage(int index)
        {
            Package toSendPackage = null;

            lock (_sendLock)
            {
                toSendPackage = _toSendPackages[index];
            }

            if (toSendPackage != null)
            {
                if (_sendBufferAction != null)
                {
                    var buffer = toSendPackage.ToBuffer();
                    _sendBufferAction(buffer, 0, buffer.Length);
                }
            }
        }

        private Task SendCleanPak(int index)
        {
            return Task.Run(() =>
            {
                //TODO 添加一个 timer

                var pak = new Package
                {
                    Flag = FlagCode.End,
                    Code = PackageCode.Clean,
                    Index = index
                };

                if (_sendBufferAction != null)
                {
                    var buffer = pak.ToBuffer();

                    _sendBufferAction(buffer, 0, buffer.Length);
                }
            });
        }

        private Task SendCleanUpPak(int index)
        {
            return Task.Run(() =>
            {
                var pak = new Package
                {
                    Flag = FlagCode.End,
                    Code = PackageCode.CleanUp,
                    Index = index
                };

                if (_sendBufferAction != null)
                {
                    var buffer = pak.ToBuffer();

                    _sendBufferAction(buffer, 0, buffer.Length);
                }
            });
        }

        #endregion

        #region 接收，和发送一样

        object _receiveLock = new object();
        int _receiveMaxIndex;

        Dictionary<int, Package> _toPackPackages;
        Dictionary<int, PackageState> _receiveMark;

        private void InitReceive()
        {
            _toPackPackages = new Dictionary<int, Package>(_options.MaxPackageBuffer * 4);
            _receiveMark = new Dictionary<int, PackageState>(_options.MaxPackageBuffer * 4);

            _receiveMaxIndex = _options.MaxPackageBuffer * 4;

            for (var i = 0; i < _options.MaxPackageBuffer * 4; i++)
            {
                _receiveMark.Add(i, PackageState.Empty);
                _toPackPackages.Add(i, null);
            }
        }

        private void TransporterReceivedBuffer(byte[] buffer, int index, int length)
        {
            //HACK 这里有一个前提是 UdpClient 处理的数据接收不完整的问题
            var pakage = new Package();
            var need = pakage.PushBuffer(buffer, ref index, length);

            if (need > 0)
            {
                _logger.LogError($"HACK 出现了 package 数据没有完整接收到的问题");
                return;
            }

            _logger.LogTrace($"接收到 {pakage}");

            Task.Run(() =>
            {
                switch (pakage.Code)
                {
                    case PackageCode.Heart:
                        DealHeart(pakage);
                        break;
                    case PackageCode.Answer:
                        DealAnswer(pakage);
                        break;

                    case PackageCode.CleanUp:
                        DealClean(pakage);
                        break;

                    case PackageCode.Text:
                    case PackageCode.ByteDescription:
                    case PackageCode.Byte:
                        DealDataPak(pakage);
                        break;
                    default:
                        _logger.LogWarning($"尚未处理 Package.Code = {pakage.Code} 类型的 package");
                        break;
                }
            });
        }

        private void DealHeart(Package package)
        {
            if (_sendBufferAction != null)
            {
                var buffer = package.ToBuffer();
                _sendBufferAction(buffer, 0, buffer.Length);
            }
        }

        private void DealAnswer(Package package)
        {
            lock (_sendLock)
            {
                if (_toSendPackages.ContainsKey(package.Index))
                {
                    _toSendPackages[package.Index] = null;
                    _sentMark[package.Index] = PackageState.Sended;
                }
            }
        }

        private void DealClean(Package package)
        {
            lock (_receiveLock)
            {
                var toClendStartIndex = package.Index;

                var cleanCount = 0;
                do
                {
                    var toCleanIndex = toClendStartIndex + cleanCount;
                    cleanCount++;

                    _receiveMark[toCleanIndex] = PackageState.Empty;
                    _toPackPackages[toCleanIndex] = null;

                } while (cleanCount < _options.MaxPackageBuffer);

                SendCleanUpPak(package.Index);
            }
        }

        private void DealCleanUp(Package package)
        {
            _continueSendingMre.Set();
        }

        private void DealDataPak(Package package)
        {
            var pakIndex = package.Index;

            SendAnswerPak(pakIndex);

            lock (_receiveLock)
            {
                if (_receiveMark[pakIndex] != PackageState.Empty)
                {
                    // 收到了重复的数据包，不做处理
                    return;
                }
            }

            lock (_receiveLock)
            {
                _receiveMark[pakIndex] = PackageState.ToPackage;
                _toPackPackages[pakIndex] = package;
            }

            if (package.Flag == FlagCode.End || package.Flag == FlagCode.Single)
            {
                TryPackPackageTask(pakIndex);
            }
        }

        private Task SendAnswerPak(int index)
        {
            return Task.Run(() =>
            {
                var pak = new Package();
                pak.Flag = FlagCode.End;
                pak.Code = PackageCode.Answer;
                pak.Index = index;

                var buffer = pak.ToBuffer();

                _sendBufferAction(buffer, 0, buffer.Length);
            });
        }

        private Task TryPackPackageTask(int finIndex)
        {
            return Task.Run(() =>
            {
                if (_toPackPackages[finIndex].Flag == FlagCode.Single)
                {
                    PackToPayloadAndDeal(finIndex);
                    return;
                }

                var can = false;
                var startIndx = (finIndex + _receiveMaxIndex - 1) % _receiveMaxIndex;

                var goon = true;

                do
                {
                    if (_receiveMark[startIndx] == PackageState.Empty)
                    {
                        goon = false;
                    }
                    else if (_toPackPackages[startIndx].Flag == FlagCode.Start)
                    {
                        goon = false;
                        can = true;
                    }
                    else
                    {
                        startIndx = (startIndx + _receiveMaxIndex - 1) % _receiveMaxIndex;
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
            Package pak = _toPackPackages[index];

            var dataPakCount = pak.PayloadLength;
            var buffer = new List<byte>();

            var lastPakCode = PackageCode.Text;

            var payload = new ProtocolPayload()
            {
                Bytes = new ByteDescription[0]
            };

            do
            {
                pak = _toPackPackages[index];

                if (pak.Code == lastPakCode)
                {
                    buffer.AddRange(pak.Data);
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
                index = (index + 1) % _receiveMaxIndex;
            }
            while (pak.Flag != FlagCode.End && pak.Flag != FlagCode.Single);

            _receivedPayloadAction(payload);
        }

        #endregion
    }
}
