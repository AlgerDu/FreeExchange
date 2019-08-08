using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Timers;

namespace D.FreeExchange.Protocol.DP
{
    public abstract class DProtocolConnecte : IProtocolConnecte
    {
        protected ILogger _logger;
        protected IProtocolCore _core;

        protected Encoding _encoding;

        readonly TryConnecteSettingItem[] _tryConnectSettings = new TryConnecteSettingItem[]
        {
            new TryConnecteSettingItem{Interval = TimeSpan.FromMilliseconds(200),TryCount = 10},
            new TryConnecteSettingItem{Interval = TimeSpan.FromSeconds(10),TryCount = 20},
            new TryConnecteSettingItem{Interval = TimeSpan.FromSeconds(20),TryCount = -1}
        };

        bool _continueSendingConnectPak;
        int _sendCount;
        int _canTryCount;
        int _currItemIndex;

        Timer timer_ContinueSendingConnectPak;

        public DProtocolConnecte(
             ILogger logger
             , IProtocolCore core
             )
        {
            _logger = logger;
            _core = core;

            _core.StateChanged += new ProtocolStateChangedEventHandler(OnStateChanged);
            _core.OptionsChanged += new ProtocolOptionsChangedEventHandler(OnOptionsChanged);

            _continueSendingConnectPak = false;
            _currItemIndex = 0;
            _canTryCount = -1;

            InitTimer();
        }

        #region IProtocolConnecte 实现
        public void DealPackage(IPackage package)
        {
            switch (package.Code)
            {
                case PackageCode.Connect:
                    DealConnect(package);
                    break;

                case PackageCode.ConnectOK:
                    DealConnectOK(package);
                    break;

                default:
                    _logger.LogError($"{this} 不处理 {package}，只处理连接相关的");
                    break;
            }
        }
        #endregion

        protected virtual void DealConnect(IPackage package)
        {
        }

        protected virtual void DealConnectOK(IPackage package)
        {
            if (_core.State == ProtocolState.Connectting)
                _core.ChangeState(ProtocolState.Online);
        }

        private void OnOptionsChanged(object sender, ProtocolOptionsChangedEventArgs e)
        {
            _encoding = e.Encoding;
        }

        private void OnStateChanged(object sender, ProtocolStateChangedEventArgs e)
        {
            if (e.NewState == ProtocolState.Connectting)
            {
                StartConnecte();
            }
            else
            {
                StopConnecte();
            }
        }

        protected virtual void StartConnecte()
        {
            _currItemIndex = 0;
            _sendCount = 0;

            var item = _tryConnectSettings[_currItemIndex];

            timer_ContinueSendingConnectPak.Interval = item.Interval.TotalMilliseconds;

            timer_ContinueSendingConnectPak.Start();

            SendConnectPackage();
        }

        protected virtual void StopConnecte()
        {
            _continueSendingConnectPak = false;
            timer_ContinueSendingConnectPak.Stop();
        }

        private void InitTimer()
        {
            timer_ContinueSendingConnectPak = new Timer();
            timer_ContinueSendingConnectPak.Elapsed += new ElapsedEventHandler((sender, e) =>
            {
                var timer = sender as Timer;

                timer.Stop();

                SendConnectPackage();

                if (_continueSendingConnectPak && _canTryCount != 0)
                {
                    if (_canTryCount > -1)
                    {
                        _sendCount++;
                        _logger.LogTrace($"{this} 发送 {_sendCount} 次连接包");
                    }

                    if (_canTryCount > 0 || _sendCount >= _canTryCount)
                    {
                        _currItemIndex++;

                        if (_currItemIndex < _tryConnectSettings.Length)
                        {
                            var item = _tryConnectSettings[_currItemIndex];

                            timer.Interval = item.Interval.TotalMilliseconds;

                            _logger.LogInformation($"{this} connec pak 发送间隔调整为 {item.Interval}");

                            _canTryCount = item.TryCount;
                            _sendCount = 0;
                        }
                        else
                        {
                            _canTryCount = 0;
                        }
                    }

                    timer.Start();
                }
            });
        }

        protected abstract void SendConnectPackage();

        protected void SendConnectOKPackage()
        {
            var package = new ConnectOkPackage();

            _core.SendPackage(package);
        }
    }

    public class DProtocolConnecte_Server : DProtocolConnecte
    {
        public DProtocolConnecte_Server(
            ILogger logger
            , IProtocolCore core
            ) : base(logger, core)
        { }

        protected override void DealConnect(IPackage package)
        {
            if (_core.State == ProtocolState.Connectting)
            {
                //说明正在处理连接包，对重复的包不做任何处理
                return;
            }
            else if (_core.State == ProtocolState.Online)
            {
                //服务端已经开始运行了，直接回复
                SendConnectOKPackage();
                return;
            }

            _core.ChangeState(ProtocolState.Connectting);

            var connectPak = package as ConnectPackage;

            var data = connectPak.GetData(_encoding);

            _core.RefreshOptions(data.Options);
        }

        protected override void SendConnectPackage()
        {
            var package = new ConnectPackage();

            var connectData = new ConnectPackageData
            {
                Uid = _core.Uid
            };

            package.SetData(connectData, _encoding);

            _core.SendPackage(package);
        }
    }

    public class DProtocolConnecte_Client : DProtocolConnecte
    {

        public DProtocolConnecte_Client(
            ILogger logger
            , IProtocolCore core
            ) : base(logger, core)
        {
        }

        protected override void StartConnecte()
        {
        }

        protected override void StopConnecte()
        {
        }

        protected override void SendConnectPackage()
        {
            var package = new ConnectPackage();

            var connectData = new ConnectPackageData
            {
                Uid = _core.Uid,
                Options = _core.Options
            };

            package.SetData(connectData, _encoding);

            _core.SendPackage(package);
        }

        /// <summary>
        /// 处理连接包
        /// </summary>
        /// <param name="package"></param>
        protected override void DealConnect(IPackage package)
        {
            //每次收到都回复一次，停止掉对面的循环
            SendConnectOKPackage();
        }
    }
}
