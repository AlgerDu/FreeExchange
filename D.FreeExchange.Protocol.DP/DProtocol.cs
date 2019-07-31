using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using D.FreeExchange.Protocol.DP;
using D.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace D.FreeExchange
{
    /// <summary>
    /// 自定义 UDP 协议构造器
    /// </summary>
    public class DProtocol : IExchangeProtocol
        , IProtocolData
    {
        ILogger _logger;

        ExchangeProtocolRunningMode _runningMode;

        Action<IProtocolPayload> _receivedPayloadAction;
        Action<int, DateTimeOffset> _receivedCmdAction;
        Action<byte[], int, int> _sendBufferAction;

        Timer timer_heart;
        DateTimeOffset _lastHeartTime;
        DateTimeOffset _lastHeartPackageTime;
        bool _lastCheckIsOnline;

        ISendPart _sendPart;
        IPayloadAnalyser _payloadAnalyser;
        IPackageFactory _pakFactory;

        #region IProtocolData

        internal string Uid { get; set; }

        internal Encoding Encoding { get; set; }

        internal ProtocolState State { get; set; }

        internal DProtocolOptions Options { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        internal IReadOnlyDictionary<int, IPackageInfo> SendingPaks => throw new NotImplementedException();

        internal IReadOnlyDictionary<int, IPackageInfo> ReceivingPaks => throw new NotImplementedException();

        #endregion

        public DProtocol(
            ILogger<DProtocol> logger
            , IOptions<DProtocolOptions> options
            )
        {
            _logger = logger;

            _shareData = new ShareData();
            _shareData.Options = options.Value;

            _sendPart = new SendPart(logger);
            _payloadAnalyser = new PayloadAnalyser(logger, _shareData);
            _pakFactory = new PackageFactory();
        }

        public override string ToString()
        {
            return $"{_shareData.Uid}";
        }

        #region IExchangeProtocol 实现
        public Task<IResult> Run(ExchangeProtocolRunningMode mode)
        {
            return Task.Run<IResult>(() =>
            {
                lock (this)
                {
                    if (_runningMode == ExchangeProtocolRunningMode.Client)
                    {
                        RunProtocol();
                    }
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
                    timer_heart?.Stop();
                    _shareData.BuilderIsRunning = false;
                    _sendPart.Clear();
                }

                return Result.CreateSuccess();
            });
        }

        public void SetReceivedPayloadAction(Action<IProtocolPayload> action)
        {
            _receivedPayloadAction = action;
        }

        public void SetReceivedCmdAction(Action<int, DateTimeOffset> action)
        {
            _receivedCmdAction = action;
        }

        public void SetSendBufferAction(Action<byte[], int, int> action)
        {
            _sendBufferAction = action;
            _sendPart.SetSendBufferAction(action);
        }

        public IResult PushBuffer(byte[] buffer, int offset, int length)
        {
            TransporterReceivedBuffer(buffer, offset, length);

            return Result.CreateSuccess();
        }

        public Task<IResult> PushPayload(IProtocolPayload payload)
        {
            return Task.Run(() =>
            {
                var paks = _payloadAnalyser.Analyse(payload);

                return _sendPart.SendPayloadPackages(paks);
            });
        }
        #endregion

        private void RunProtocol()
        {
            InitAndRunHeartTimer();

            _shareData.BuilderIsRunning = true;

            _sendPart.Init(_shareData);
        }

        private void InitAndRunHeartTimer()
        {
            timer_heart = new Timer();
            timer_heart.Interval = TimeSpan.FromSeconds(_shareData.Options.HeartInterval).TotalMilliseconds;

            timer_heart.Elapsed += (sender, e) =>
            {
                CheckIsOnline();
            };

            if (_runningMode == ExchangeProtocolRunningMode.Client)
            {
                timer_heart.Elapsed += (sender, e) =>
                {
                    SendHeartPackage();
                };
            }

            timer_heart.Start();
        }

        private void CheckIsOnline()
        {
            var isOnline = DateTimeOffset.Now - _lastHeartTime < TimeSpan.FromSeconds(_shareData.Options.HeartInterval * 2);

            if (isOnline != _lastCheckIsOnlibe)
            {
                var cmd = isOnline ? ExchangeProtocolCmd.BackOnline : ExchangeProtocolCmd.Offline;

                NotifyCmd(cmd);

                switch (cmd)
                {
                    case ExchangeProtocolCmd.Offline:
                        _sendPart.Stop();
                        break;

                    case ExchangeProtocolCmd.BackOnline:
                        SendConnectPackage();
                        break;

                    default:
                        break;
                }
            }

            _lastCheckIsOnlibe = isOnline;
        }

        private async void NotifyCmd(ExchangeProtocolCmd cmd)
        {
            await Task.Run(() =>
            {
                var cmdTime = DateTimeOffset.Now;

                try
                {
                    _receivedCmdAction?.Invoke((int)cmd, cmdTime);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"在发送 cmd {cmd} {cmdTime} 时出现异常：{ex}");
                }
            });
        }

        /// <summary>
        /// 发送心跳包
        /// </summary>
        /// <param name="heart">有参时用于回复心跳包</param>
        private void SendHeartPackage(IPackage heart = null)
        {
            if (heart == null)
            {
                heart = new HeartPackage()
                {
                    HeartTime = DateTimeOffset.Now
                };
            }

            SendPackage(heart);
        }

        private async void SendConnectPackage()
        {
            await Task.Run(() =>
            {
                var package = new ConnectPackage(_shareData.Encoding);

                var connectData = new ConnectPackageData
                {
                    Uid = _shareData.Uid
                };

                if (_runningMode == ExchangeProtocolRunningMode.Client)
                {
                    connectData.Options = _shareData.Options;
                }

                SendPackage(package);
            });
        }

        /// <summary>
        /// 将 package 转换成 buffer 通过 action 发送出去
        /// </summary>
        /// <param name="package"></param>
        private async void SendPackage(IPackage package)
        {
            await Task.Run(() =>
            {
                try
                {
                    _sendBufferAction?.Invoke(package.ToBuffer(), 0, 0);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"在发送 package {package.Code} 数据的过程中出现异常：{ex}");
                }
            });
        }

        /// <summary>
        /// 将 buffer 组装成 package；再根据不同的 package code 分散处理
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="index"></param>
        /// <param name="length"></param>
        private void TransporterReceivedBuffer(byte[] buffer, int index, int length)
        {
            //HACK 这里有一个前提是 UdpClient 处理的数据接收不完整的问题
            // 经过学习，对于 Udp 来说，应该确实是完整的
            var pakage = _pakFactory.CreatePackage(buffer[index++]);

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
                    case PackageCode.Connect:
                        DealConnect(pakage);
                        break;

                    case PackageCode.Heart:
                        DealHeart(pakage);
                        break;

                    case PackageCode.Answer:
                        DealAnswer(pakage);
                        break;

                    case PackageCode.Clean:
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

        /// <summary>
        /// 处理收到的心跳包
        /// </summary>
        /// <param name="package"></param>
        private void DealHeart(IPackage package)
        {
            var heart = package as HeartPackage;

            if (heart.HeartTime < _lastHeartPackageTime)
            {
                _logger.LogWarning($"{this} 接收到无效的心跳包");
            }
            else
            {
                _lastHeartTime = DateTimeOffset.Now;

                NotifyCmd(ExchangeProtocolCmd.Heart);

                if (_runningMode == ExchangeProtocolRunningMode.Server)
                {
                    SendHeartPackage(package);
                }
            }
        }

        /// <summary>
        /// 处理连接包
        /// </summary>
        /// <param name="package"></param>
        private void DealConnect(IPackage package)
        {
            if (_runningMode == ExchangeProtocolRunningMode.Server)
            {
                var connect = package as ConnectPackage;
                connect.Encoding = _shareData.Encoding;

                var connectData = connect.Data;

                _shareData.Options = connectData.Options;

                _sendPart.Init(_shareData);

                InitAndRunHeartTimer();

                SendConnectPackage();
            }

            _sendPart.Run();
        }

        /// <summary>
        /// server mode 下，处理心跳
        /// </summary>
        /// <param name="package"></param>
        private void DealHeartWhenServerMode(IPackage package)
        {
            if (_sendBufferAction != null)
            {
                var buffer = package.ToBuffer();
                _sendBufferAction(buffer, 0, buffer.Length);
            }
        }

        /// <summary>
        /// server mode 下，处理心跳
        /// </summary>
        /// <param name="package"></param>
        private void DealHeartWhenClientMode(IPackage package)
        {
            if (_sendBufferAction != null)
            {
                var buffer = package.ToBuffer();
                _sendBufferAction(buffer, 0, buffer.Length);
            }
        }

        private void DealAnswer(IPackage pak)
        {
            _sendPart.ReceiveAnswer((pak as IPackageWithIndex).Index);
        }

        private void DealClean(IPackage package)
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

        private void DealDataPak(IPackage package)
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
    }
}
