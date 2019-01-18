using System;
using System.Collections.Generic;
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
        ITransporter _transporter;

        DProtocolBuilderOptions _options;

        bool _builderRunning;

        public DProtocolBuilder(
            ILogger<DProtocolBuilder> logger
            , IOptions<DProtocolBuilderOptions> options
            , ITransporter transporter
            )
        {
            _logger = logger;
            _transporter = transporter;

            _options = options.Value;

            _builderRunning = false;

            InitSend();
        }

        #region IProtocolBuilder 实现
        public IProtocolBuilder Run()
        {
            throw new NotImplementedException();
        }

        public Task<IResult> SendAsync(IProtocolPayload payload)
        {
            return Task.Run(() => AnalysePayloadToPackage(payload));
        }

        public void SetControlReceiveAction(Action<int> action)
        {
            throw new NotImplementedException();
        }

        public void SetPayloadReceiveAction(Action<IProtocolPayload> action)
        {
            throw new NotImplementedException();
        }

        public IProtocolBuilder Stop()
        {
            throw new NotImplementedException();
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

        /// <summary>
        /// 初始化 send 相关的一些数据
        /// </summary>
        private void InitSend()
        {
            _toDistributeIndexPackages = new Queue<Package>(_options.MaxPackageBuffer);
            _toSendPackages = new Dictionary<int, Package>(_options.MaxPackageBuffer);

            _distributeMre = new ManualResetEvent(false);
            _morePackagesMre = new ManualResetEvent(false);
            _morePaksToSendMre = new ManualResetEvent(false);

            _maxSendIndex = _options.MaxPackageBuffer;
            _currSendIndex = 0;
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
                    return Result.CreateError();
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
                    _toSendPackages.Add(_currSendIndex, toDistributeIndexPackage);
                    _morePaksToSendMre.Set();

                    _currSendIndex++;
                }
            }
        }

        private void SendTask()
        {
            while (_builderRunning)
            {
                if (_)
            }
        }

        #endregion

        #region 接收，和发送一样

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

                packages.Add(package);

            } while (offeset < buffer.Length);

            return packages;
        }
    }
}
