using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Timers;

namespace D.FreeExchange.Protocol.DP
{
    /// <summary>
    /// 心跳
    /// </summary>
    public abstract class DProtocolHeart : IProtocolHeart
    {
        protected ILogger _logger;
        protected IProtocolCore _core;

        protected Timer timer_heart;
        DateTimeOffset _lastHeartTime;
        DateTimeOffset _lastHeartPackageTime;
        protected bool _lastCheckIsOnline;

        TimeSpan _heartInterval;

        public DProtocolHeart(
            ILogger logger
            , IProtocolCore core
            )
        {
            _logger = logger;
            _core = core;

            _core.OptionsChanged += new ProtocolOptionsChangedEventHandler(OnOptionsChanged);
            _core.StateChanged += new ProtocolStateChangedEventHandler(OnStateChanged);

            InitHeartTimer();
        }

        public override string ToString()
        {
            return $"{_core} heart";
        }

        public virtual void DealHerat(IPackage package)
        {
            var heart = package as HeartPackage;

            if (heart.HeartTime < _lastHeartPackageTime)
            {
                _logger.LogWarning($"{this} 接收到无效的心跳包");
            }
            else
            {
                _lastHeartTime = DateTimeOffset.Now;
            }
        }

        protected void OnStateChanged(object sender, ProtocolStateChangedEventArgs e)
        {
            if (e.NewState == ProtocolState.Closing || e.NewState == ProtocolState.Stop)
            {
                timer_heart.Stop();
                _logger.LogTrace($"{this} 由状态 {e.OldState} => {e.NewState} 停止心跳定时器");
            }
            else if (e.OldState == ProtocolState.Stop)
            {
                StartTimer();
                _logger.LogTrace($"{this} 由状态 {e.OldState} => {e.NewState} 开启心跳定时器");
            }
        }

        protected void OnOptionsChanged(object sender, ProtocolOptionsChangedEventArgs e)
        {
            _logger.LogInformation($"{this} 心跳间隔由 {_heartInterval.TotalSeconds} 跟新为 {e.Options.HeartInterval} (S)");

            _heartInterval = TimeSpan.FromSeconds(e.Options.HeartInterval);

            timer_heart.Interval = _heartInterval.TotalMilliseconds;
        }

        protected abstract void InitHeartTimer();

        protected virtual void StartTimer()
        {
            timer_heart.Start();
        }

        /// <summary>
        /// 检测一定时间内有无心跳包，来判定是否在线
        /// </summary>
        protected void CheckIsOnline()
        {
            var isOnline = DateTimeOffset.Now - _lastHeartTime < _heartInterval;

            if (isOnline != _lastCheckIsOnline && !isOnline)
            {
                _core.ChangeState(ProtocolState.Offline);
            }

            _lastCheckIsOnline = isOnline;
        }

        /// <summary>
        /// 发送心跳包
        /// </summary>
        /// <param name="heart">有参时用于回复心跳包</param>
        protected void SendHeartPackage(IPackage heart = null)
        {
            if (heart == null)
            {
                heart = new HeartPackage()
                {
                    HeartTime = DateTimeOffset.Now
                };
            }

            _core.SendPackage(heart);
        }
    }

    public class DProtocolHeart_Client : DProtocolHeart
    {
        public DProtocolHeart_Client(
            ILogger logger
            , IProtocolCore core
            ) : base(logger, core)
        {

        }

        protected override void InitHeartTimer()
        {
            timer_heart = new Timer();

            timer_heart.Elapsed += new ElapsedEventHandler((sender, e) =>
            {
                CheckIsOnline();
            });

            timer_heart.Elapsed += (sender, e) =>
            {
                SendHeartPackage();
            };
        }

        protected override void StartTimer()
        {
            //客户端，定时器启动的同时就需要发送一次心跳包
            SendHeartPackage();

            base.StartTimer();
        }

        public override void DealHerat(IPackage package)
        {
            if (!_lastCheckIsOnline)
            {
                _lastCheckIsOnline = true;

                _core.ChangeState(ProtocolState.Connectting);
                _core.NotifyCmd(ExchangeProtocolCmd.BackOnline, DateTimeOffset.Now);
            }

            base.DealHerat(package);
        }
    }

    public class DProtocolHeart_Server : DProtocolHeart
    {
        public DProtocolHeart_Server(
            ILogger logger
            , IProtocolCore core
            ) : base(logger, core)
        {

        }

        protected override void InitHeartTimer()
        {
            timer_heart = new Timer();

            timer_heart.Elapsed += new ElapsedEventHandler((sender, e) =>
            {
                CheckIsOnline();
            });
        }

        public override void DealHerat(IPackage package)
        {
            //服务端收到心跳之后，需要立马将受到的心跳包回复回去
            _core.SendPackage(package);

            base.DealHerat(package);
        }
    }
}
