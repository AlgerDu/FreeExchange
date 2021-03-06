﻿using System;
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
    public class DProtocol
        : IExchangeProtocol
        , IProtocolCore
    {
        //经过整理之后的这个类，其实只是一个外壳
        //想了下，觉得既是核心又是壳，会好写很多

        readonly Dictionary<ProtocolState, IEnumerable<ProtocolState>> _stateChangeRules = new Dictionary<ProtocolState, IEnumerable<ProtocolState>>
        {
            { ProtocolState.Stop , new ProtocolState[]{
                    ProtocolState.Offline
                }
            },
            { ProtocolState.Offline , new ProtocolState[]{
                    ProtocolState.Stop,
                    ProtocolState.Connectting
                }
            },
            { ProtocolState.Connectting , new ProtocolState[]{
                    ProtocolState.Stop,
                    ProtocolState.Online
                }
            },
            { ProtocolState.Online , new ProtocolState[]{
                    ProtocolState.Stop,
                    ProtocolState.Offline
                }
            }
        };

        ILogger _logger;
        DProtocolOptions _options;

        Action<IProtocolPayload> _receivedPayloadAction;
        Action<int, DateTimeOffset> _receivedCmdAction;
        Action<byte[], int, int> _sendBufferAction;

        IPayloadAnalyser _payloadAnalyser;
        IPackageFactory _pakFactory;

        IProtocolConnecte _connecte;
        IProtocolHeart _heart;
        IProtocolSend _send;
        IProtocolReceive _receive;

        ExchangeProtocolRunningMode _runningMode;
        ProtocolState _state;

        Encoding _encoding;
        string _uid;

        public DProtocol(
            ILogger<DProtocol> logger
            , ExchangeProtocolRunningMode runningMode
            , IOptions<DProtocolOptions> options
            )
        {
            _encoding = Encoding.Default;
            _uid = $"{Guid.NewGuid().ToString()}";

            _logger = logger;
            _options = options.Value;
            _runningMode = runningMode;

            _payloadAnalyser = new PayloadAnalyser(logger, this);
            _pakFactory = new PackageFactory();

            _send = new DProtocoloSend(logger, this);
            _receive = new DProtocoloReceive(logger, this);

            _state = ProtocolState.Stop;

            StateChanged += new ProtocolStateChangedEventHandler(OnStateChanged);
        }

        public override string ToString()
        {
            return $"DP[{_runningMode},{_uid}]";
        }

        #region IExchangeProtocol 实现
        public Task<IResult> Run()
        {
            return Task.Run<IResult>(() =>
            {
                lock (this)
                {
                    if (_runningMode == ExchangeProtocolRunningMode.Client)
                    {
                        _heart = new DProtocolHeart_Client(_logger, this);
                        _connecte = new DProtocolConnecte_Client(_logger, this);

                    }
                    else
                    {
                        _heart = new DProtocolHeart_Server(_logger, this);
                        _connecte = new DProtocolConnecte_Server(_logger, this);
                    }

                    RefreshOptions(_options);

                    ChangeState(ProtocolState.Offline);
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
                    ChangeState(ProtocolState.Stop);
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

                return _send.DistributeThenSendPackages(paks);
            });
        }

        #endregion

        #region IProtocolCore 实现
        public string Uid => _uid;

        public ProtocolState State => _state;

        public DProtocolOptions Options => _options;

        public event ProtocolStateChangedEventHandler StateChanged;
        public event ProtocolOptionsChangedEventHandler OptionsChanged;

        public void ChangeState(ProtocolState newState)
        {
            lock (this)
            {
                if (_state == newState)
                {
                    _logger.LogWarning($"{this} 相同的状态不可以转换：{newState}");
                    return;
                }

                var canChange = _stateChangeRules[_state].Where(ss => ss == newState).Count() == 1;

                if (!canChange)
                {
                    _logger.LogWarning($"{this} {_state} 不能变换到 {newState} 状态");
                    return;
                }

                var oldState = _state;
                _state = newState;

                _logger.LogInformation($"{this} state {oldState} => {newState}");

                StateChanged?.Invoke(this, new ProtocolStateChangedEventArgs
                {
                    OldState = oldState,
                    NewState = newState,
                    Time = DateTimeOffset.Now
                });
            }
        }

        public Task DealProtocolPayload(IProtocolPayload payload)
        {
            return Task.Run(() =>
            {
                try
                {
                    _receivedPayloadAction(payload);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"{this} 在处理 IProtocolPayload 时出现异常：{ex}");
                }
            });
        }

        public Task NotifyCmd(ExchangeProtocolCmd cmd, DateTimeOffset time)
        {
            return Task.Run(() =>
            {
                try
                {
                    _receivedCmdAction.Invoke((int)cmd, time);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"{this} 在发送 cmd {cmd} {time} 时出现异常：{ex}");
                }
            });
        }

        public void RefreshOptions(DProtocolOptions options)
        {
            lock (this)
            {
                _logger.LogTrace($"{this} 刷新配置参数");

                _options = options;

                OptionsChanged?.Invoke(this, new ProtocolOptionsChangedEventArgs
                {
                    Encoding = _encoding,
                    Options = _options
                });
            }
        }

        public Task SendPackage(IPackage pak)
        {
            return Task.Run(() =>
            {
                try
                {
                    var buffer = pak.ToBuffer();
                    _sendBufferAction.Invoke(buffer, 0, buffer.Length);

                    _logger.LogTrace($"{this} 发送 {pak}");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"{this} 在发送 {pak} 的过程中出现异常：{ex}");
                }
            });
        }

        #endregion

        protected void OnStateChanged(object sender, ProtocolStateChangedEventArgs e)
        {
            if (e.NewState == ProtocolState.Offline && _runningMode == ExchangeProtocolRunningMode.Client)
            {
                ChangeState(ProtocolState.Connectting);
            }
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
                _logger.LogError($"{this} HACK 出现了 package 数据没有完整接收到的问题");
                return;
            }

            _logger.LogTrace($"{this} 接收到 {pakage}");

            Task.Run(() =>
            {
                switch (pakage.Code)
                {
                    case PackageCode.Connect:
                    case PackageCode.ConnectOK:
                        _connecte.DealPackage(pakage);
                        break;

                    case PackageCode.Heart:
                        _heart.DealHerat(pakage);
                        break;

                    case PackageCode.Answer:
                        _send.DealAnswer(pakage);
                        break;

                    case PackageCode.Clean:

                    case PackageCode.Text:
                    case PackageCode.ByteDescription:
                    case PackageCode.Byte:
                        _receive.DealIndexPackage(pakage);
                        break;
                    default:
                        _logger.LogWarning($"尚未处理 Package.Code = {pakage.Code} 类型的 package");
                        break;
                }
            });
        }
    }
}
