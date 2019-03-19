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
            BufferToPackage(packages, textBuffer, PackageCode.Text);

            lock (_sendLock)
            {
                if (_toDistributeIndexPackages.Count + packages.Count > _options.MaxPackageBuffer)
                {
                    return Result.CreateError("缓存已满");
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

        private void DistributeIndexTask()
        {
            while (_builderRunning)
            {
                if (_currSendIndex % _options.MaxPackageBuffer == 0)
                {
                    CleanAndRepeat().Wait();
                }

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
            }
        }

        private Task CleanAndRepeat()
        {
            return Task.Run(() =>
            {
                var cleanCount = 0;
                do
                {
                    var toCleanIndex = (_currSendIndex + cleanCount) % _maxSendIndex;

                    _toSendPackages[toCleanIndex] = null;
                    _sentMark[toCleanIndex] = PackageState.Empty;

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
                        need = _sentMark[toRepeatIndex] != PackageState.Sended;
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

        #endregion

        #region 接收，和发送一样

        object _receiveLock = new object();
        int _receiveMinIndex;
        int _receiveMaxIndex;

        Dictionary<int, Package> _toPackPackages;
        Dictionary<int, int> _receiveMark;

        private void InitReceive()
        {
            _toPackPackages = new Dictionary<int, Package>(_options.MaxPackageBuffer * 4);
            _receiveMark = new Dictionary<int, int>(_options.MaxPackageBuffer * 4);

            _receiveMaxIndex = 0;
            _receiveMaxIndex = _options.MaxPackageBuffer;

            for (var i = 0; i < _options.MaxPackageBuffer * 4; i++)
            {
                _receiveMark.Add(i, 0);
            }
        }

        private void TransporterReceivedBuffer(byte[] buffer, int index, int length)
        {
            //HACK 这里有一个前提是 UdpClient 处理的数据接收不完整的问题
            var pakage = new Package();
            var need = pakage.PushBuffer(buffer, ref index, length);

            if (need > 0)
            {
                _logger.LogError($"HACK 出现了 udp 数据没有完整接收到的问题");
                return;
            }

            _logger.LogTrace($"接收到 {pakage}");

            switch (pakage.Code)
            {
                case PackageCode.Heart:
                    DealHeart(pakage);
                    break;
                case PackageCode.Answer:
                    DealAnswer(pakage);
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
        }

        private void DealHeart(Package package)
        {
            var buffer = package.ToBuffer();
            if (_sendBufferAction != null)
            {
                _sendBufferAction(buffer, 0, buffer.Length);
            }
        }

        private void DealAnswer(Package package)
        {
            lock (_sendLock)
            {
                if (_toSendPackages.ContainsKey(package.Index))
                {
                    _toSendPackages.Remove(package.Index);
                }
            }
        }

        private void DealDataPak(Package package)
        {
            var pakIndex = package.Index;

            if (pakIndex >= _receiveMaxIndex
             && pakIndex < _receiveMaxIndex + _options.MaxPackageBuffer)
            {
                lock (_receiveLock)
                {
                    _receiveMinIndex = _receiveMaxIndex;
                    _receiveMaxIndex += _options.MaxPackageBuffer;

                    for (var key = _receiveMinIndex; key < _receiveMaxIndex; key++)
                    {
                        _receiveMark[key] = 0;

                        if (_toPackPackages.ContainsKey(key))
                        {
                            _toPackPackages.Remove(key);
                        }
                    }
                }
            }
            else if (pakIndex < _options.MaxPackageBuffer
                     && _receiveMaxIndex == _options.MaxPackageBuffer * 4)
            {
                lock (_receiveLock)
                {
                    _receiveMinIndex = 0;
                    _receiveMaxIndex = _options.MaxPackageBuffer;

                    for (var key = _receiveMinIndex; key < _receiveMaxIndex; key++)
                    {
                        _receiveMark[key] = 0;

                        if (_toPackPackages.ContainsKey(key))
                        {
                            _toPackPackages.Remove(key);
                        }
                    }
                }
            }

            lock (_receiveLock)
            {
                if (_receiveMark[pakIndex] == 0)
                {
                    _toPackPackages.Add(pakIndex, package);

                    SendAnswerPak(pakIndex);

                    if (package.Fin)
                    {
                        Task.Run(() => TryPackPackageTask(pakIndex));
                    }
                }
            }
        }

        private Task SendAnswerPak(int index)
        {
            return Task.Run(() =>
            {
                var pak = new Package();
                pak.Fin = true;
                pak.Code = PackageCode.Answer;
                pak.Index = index;

                var buffer = pak.ToBuffer();

                _sendBufferAction?.Invoke(buffer, 0, buffer.Length);
            });
        }

        private void TryPackPackageTask(int finIndex)
        {
            var can = false;
            var offset = finIndex - 1;

            while (true)
            {
                var mark = _receiveMark[offset];
                if (mark == 0)
                {
                    break;
                }
                else if (mark == 2)
                {
                    can = true;
                    break;
                }
                else
                {
                    offset--;
                    if (offset == 0) offset = _options.MaxPackageBuffer * 4 - 1;
                }
            }

            if (can)
            {
                List<Package> tmpPackages = new List<Package>();

                lock (_receiveLock)
                {
                    while (offset != finIndex)
                    {
                        offset += 1;

                        if (offset == _options.MaxPackageBuffer * 4) offset = 0;

                        tmpPackages.Add(_toPackPackages[offset]);
                        _toPackPackages.Remove(offset);
                    }
                }

                var code = PackageCode.Text;
                List<byte> buffer = new List<byte>();
                var payload = new ProtocolPayload();

                foreach (var package in tmpPackages)
                {
                    if (code == package.Code)
                    {
                        buffer.AddRange(package.Data);
                    }
                    else
                    {
                        PackBufferToPayload(buffer.ToArray(), code, payload);

                        buffer.Clear();
                        code = package.Code;
                    }
                }

                PackBufferToPayload(buffer.ToArray(), code, payload);

                _receivedPayloadAction?.Invoke(payload);
            }
        }

        private void PackBufferToPayload(byte[] buffer, PackageCode code, ProtocolPayload payload)
        {
            if (code == PackageCode.Text)
            {
                payload.Text = _encoding.GetString(buffer.ToArray());
            }
        }

        #endregion

        private void BufferToPackage(List<Package> packages, byte[] buffer, PackageCode code)
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

                if (offeset >= buffer.Length) package.Fin = true;
                packages.Add(package);


            } while (offeset < buffer.Length);
        }
    }
}
