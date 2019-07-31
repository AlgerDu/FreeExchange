﻿using D.FreeExchange.Protocol.DP;
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

                switch (cmd)
                {
                    case ExchangeProtocolCmd.Offline:
                        Send_Pause();
                        break;

                    case ExchangeProtocolCmd.BackOnline:
                        SendConnectPackage();
                        break;

                    default:
                        break;
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
    }
}
