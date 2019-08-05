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
    /// 自定义 UDP 协议；
    /// </summary>
    public class DProtocol : IExchangeProtocol
    {
        //经过整理之后的这个类，其实只是一个外壳

        ILogger _logger;
        DProtocolOptions _options;

        ExchangeProtocolRunningMode _runningMode;

        Action<IProtocolPayload> _receivedPayloadAction;
        Action<int, DateTimeOffset> _receivedCmdAction;
        Action<byte[], int, int> _sendBufferAction;

        IPayloadAnalyser _payloadAnalyser;
        IPackageFactory _pakFactory;

        IProtocolCore _core;

        IDProtocolHeart _heart;
        IDProtocolSend _send;
        IDProtocolReceive _receive;

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

            _lastCleanIndex = 0;
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
                State = ProtocolState.Offline;
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
                State = ProtocolState.Connectting;
            }

            SendConnectPackage();
        }

        private void ChangeToOnline()
        {
            lock (this)
            {
                State = ProtocolState.Online;
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
                    var buffer = package.ToBuffer();
                    _sendBufferAction?.Invoke(buffer, 0, buffer.Length);
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

            var need = pakage.PushBuffer(buffer, ref index, length - 1);

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

                    case PackageCode.ConnectOK:
                        DealConnectOK(pakage);
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
            ReceiveAnswer((pak as IPackageWithIndex).Index);
        }
    }
}
