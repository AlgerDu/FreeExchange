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
    public partial class DProtocol : IExchangeProtocol
    {
        ILogger _logger;

        ExchangeProtocolRunningMode _runningMode;

        Action<IProtocolPayload> _receivedPayloadAction;
        Action<int, DateTimeOffset> _receivedCmdAction;
        Action<byte[], int, int> _sendBufferAction;

        IPayloadAnalyser _payloadAnalyser;
        IPackageFactory _pakFactory;

        public DProtocol(
            ILogger<DProtocol> logger
            , IOptions<DProtocolOptions> options
            )
        {
            _encoding = Encoding.Default;
            _uid = $"DP[{Guid.NewGuid().ToString()}]";

            _logger = logger;

            _options = options.Value;

            _payloadAnalyser = new PayloadAnalyser(logger);
            _pakFactory = new PackageFactory();

            _state = ProtocolState.Stop;
        }

        public override string ToString()
        {
            return $"{_uid}";
        }

        #region IExchangeProtocol 实现
        public Task<IResult> Run(ExchangeProtocolRunningMode mode)
        {
            return Task.Run<IResult>(() =>
            {
                lock (this)
                {
                    _runningMode = mode;

                    ChangeToOffline();
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
                    _state = ProtocolState.Stop;

                    timer_heart?.Stop();

                    Send_Clear();
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

                return SendPayloadPackages(paks);
            });
        }

        #endregion

        /// <summary>
        /// 准备好相互连接前的准备
        /// </summary>
        private void PrepareToRunProtocol()
        {
            _state = ProtocolState.Connectting;

            ResetOptions(_options);
            InitAndRunHeartTimer();

            Send_Init();
            Send_Run();
        }

        private void ChangeToOffline()
        {
            var old = ProtocolState.Stop;
            lock (this)
            {
                old = _state;
                _state = ProtocolState.Offline;
            }

            if (old == ProtocolState.Stop)
            {
                if (_runningMode == ExchangeProtocolRunningMode.Client)
                {
                    PrepareToRunProtocol();
                }
            }
            else if (old == ProtocolState.Online)
            {
                Send_Pause();
            }
        }

        private void ChangeToConnectting()
        {
            lock (this)
            {
                _state = ProtocolState.Connectting;
            }

            SendConnectPackage();
        }

        private void ChangeToOnline()
        {
            lock (this)
            {
                _state = ProtocolState.Online;
            }

            Send_Run();
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

        private async void SendConnectPackage()
        {
            await Task.Run(() =>
            {
                //如果是连接状态就一直发送
                while (_state == ProtocolState.Connectting)
                {
                    var package = new ConnectPackage(_encoding);

                    var connectData = new ConnectPackageData
                    {
                        Uid = _uid
                    };

                    if (_runningMode == ExchangeProtocolRunningMode.Client)
                    {
                        connectData.Options = _options;
                    }

                    SendPackage(package);

                    System.Threading.Thread.Sleep(TimeSpan.FromMilliseconds(50));
                }
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
        /// 处理连接包
        /// </summary>
        /// <param name="package"></param>
        private void DealConnect(IPackage package)
        {
            //每次收到都回复一次，停止掉对面的循环
            SendPackage(new ConnectOkPackage());

            if (_runningMode == ExchangeProtocolRunningMode.Server)
            {
                var connect = package as ConnectPackage;
                connect.Encoding = _encoding;

                var connectData = connect.Data;

                ResetOptions(connectData.Options);

                SendConnectPackage();
            }
        }

        /// <summary>
        /// 处理连接成功
        /// </summary>
        /// <param name="package"></param>
        private void DealConnectOK(IPackage package)
        {
            ChangeToOnline();
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
