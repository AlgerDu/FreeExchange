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
                    Task.Run(() => SendTask());
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

                    _distributeMre.Set();
                    _morePackagesMre.Set();
                    _morePaksToSendMre.Set();
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

        ManualResetEvent _distributeMre;
        ManualResetEvent _morePackagesMre;
        ManualResetEvent _morePaksToSendMre;

        /// <summary>
        /// 等待分配 index 的 packages
        /// </summary>
        Queue<Package> _toDistributeIndexPackages;

        /// <summary>
        /// 已经分配好 index 等待发送的 packages
        /// </summary>
        Dictionary<int, Package> _toSendPackages;
        Dictionary<int, int> _sentMark;

        /// <summary>
        /// 初始化 send 相关的一些数据
        /// </summary>
        private void InitSend()
        {
            _toDistributeIndexPackages = new Queue<Package>(_options.MaxPackageBuffer);
            _toSendPackages = new Dictionary<int, Package>(_options.MaxPackageBuffer * 4);
            _sentMark = new Dictionary<int, int>(_options.MaxPackageBuffer * 4);

            _distributeMre = new ManualResetEvent(false);
            _morePackagesMre = new ManualResetEvent(false);
            _morePaksToSendMre = new ManualResetEvent(false);

            _maxSendIndex = _options.MaxPackageBuffer;
            _currSendIndex = 0;

            for (var i = 0; i < _options.MaxPackageBuffer * 4; i++)
            {
                _sentMark.Add(i, 0);
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

                    _morePackagesMre.Set();

                    return Result.CreateSuccess();
                }
            }
        }

        private void DistributeIndexTask()
        {
            while (_builderRunning)
            {
                if (_currSendIndex == _maxSendIndex)
                {
                    _distributeMre.WaitOne();
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
                    _morePackagesMre.WaitOne();
                }
                else
                {
                    toDistributeIndexPackage.Index = _currSendIndex;

                    lock (_sendLock)
                    {
                        _toSendPackages.Add(_currSendIndex, toDistributeIndexPackage);
                        _sentMark[_currSendIndex] = 1;
                        _currSendIndex++;
                    }

                    _morePaksToSendMre.Set();
                }
            }
        }

        private void SendTask()
        {
            while (_builderRunning)
            {
                lock (_sendLock)
                {
                    if (_currSendIndex == _maxSendIndex && _toSendPackages.Count == 0)
                    {
                        if (_maxSendIndex >= _options.MaxPackageBuffer * 4)
                        {
                            _maxSendIndex = _options.MaxPackageBuffer;
                            _currSendIndex = 0;
                        }
                        else
                        {
                            _maxSendIndex += _options.MaxPackageBuffer;
                        }

                        _morePackagesMre.Set();
                    }
                }

                IEnumerable<int> toSendPakIndexs;

                lock (_toSendPackages)
                {
                    toSendPakIndexs = _sentMark.Keys.Where(kk => _sentMark[kk] == 1).ToArray();
                }

                if (toSendPakIndexs.Count() <= 0)
                {
                    _morePaksToSendMre.WaitOne();
                }

                foreach (var index in toSendPakIndexs)
                {
                    lock (_sendLock)
                    {
                        if (_toSendPackages.ContainsKey(index))
                        {
                            var pak = _toSendPackages[index];

                            var buffer = pak.ToBuffer();

                            //_sendBufferAction?.BeginInvoke(buffer, 0, buffer.Length, _sendBufferAction.EndInvoke, null);
                            if (_sendBufferAction != null)
                            {
                                _sendBufferAction(buffer, 0, buffer.Length);
                            }

                            _sentMark[index] = 2;
                        }
                    }
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
