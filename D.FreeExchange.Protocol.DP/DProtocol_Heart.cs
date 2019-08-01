using D.FreeExchange.Protocol.DP;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Timers;

namespace D.FreeExchange
{
    /// <summary>
    /// 心跳相关的内容
    /// </summary>
    public partial class DProtocol
    {
        Timer timer_heart;
        DateTimeOffset _lastHeartTime;
        DateTimeOffset _lastHeartPackageTime;
        bool _lastCheckIsOnline;

        /// <summary>
        /// 初始化并且运行心跳定时器
        /// </summary>
        private void InitAndRunHeartTimer()
        {
            timer_heart = new Timer();
            timer_heart.Interval = TimeSpan.FromSeconds(_options.HeartInterval).TotalMilliseconds;

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

        /// <summary>
        /// 检测是否在线
        /// </summary>
        private void CheckIsOnline()
        {
            var isOnline = DateTimeOffset.Now - _lastHeartTime < TimeSpan.FromSeconds(_options.HeartInterval * 2);

            if (isOnline != _lastCheckIsOnline)
            {
                var cmd = isOnline ? ExchangeProtocolCmd.BackOnline : ExchangeProtocolCmd.Offline;

                NotifyCmd(cmd);

                if (isOnline)
                {
                    ChangeToConnectting();
                }
                else
                {
                    ChangeToOffline();
                }
            }

            _lastCheckIsOnline = isOnline;
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
    }
}
