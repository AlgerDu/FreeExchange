using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using D.FreeExchange.Protocol.DP;
using D.Utils;
using Microsoft.Extensions.Logging;

namespace D.FreeExchange
{
    public partial class DProtocol
    {
        ManualResetEvent mre_MorePaksToDistrubute;
        ManualResetEvent mre_ContinueSending;
        Queue<PackageWithPayload> _toDistributeIndexPaks;

        HashSet<int> _toRepeatSendPakIDs;

        int _currIndex;
        int _maxSendIndex;

        System.Timers.Timer timer_RepeatSendPaks;

        /// <summary>
        /// send 部分初始化
        /// </summary>
        private void Send_Init()
        {
            mre_MorePaksToDistrubute = new ManualResetEvent(true);
            mre_ContinueSending = new ManualResetEvent(true);

            timer_RepeatSendPaks = new System.Timers.Timer();
            timer_RepeatSendPaks.Elapsed += new System.Timers.ElapsedEventHandler(RepeatSendPaks);

            _toRepeatSendPakIDs = new HashSet<int>();

            var maxPakBuffer = _options.MaxPackageBuffer;

            _currIndex = 0;
            _maxSendIndex = maxPakBuffer * 4;
            _toDistributeIndexPaks = new Queue<PackageWithPayload>(maxPakBuffer);

            timer_RepeatSendPaks.Interval = _options.PaylodPakRepeatSendInterval;
        }

        /// <summary>
        /// send 部分开始运行
        /// </summary>
        private void Send_Run()
        {
            lock (this)
            {
                timer_RepeatSendPaks.Start();

                Task.Run(() => DistributeIndexTask());
            }
        }

        /// <summary>
        /// send 部分停止运行
        /// </summary>
        private void Send_Stop()
        {
            lock (this)
            {
                timer_RepeatSendPaks.Stop();
            }
        }

        /// <summary>
        /// 暂定发送
        /// </summary>
        private void Send_Pause()
        {

        }

        private void Send_Clear()
        {
            Send_Stop();

            mre_MorePaksToDistrubute.Set();
            mre_ContinueSending.Set();
        }

        private void ReceiveAnswer(int pakIndex)
        {
            ReceivedIndexPak(pakIndex);
        }

        private void ContinueSending()
        {
            mre_ContinueSending.Set();
        }

        private void ReceivedIndexPak(int pakIndex)
        {
            var pakInfo = _sendingPaks[pakIndex];

            if (pakInfo.Package.Code == PackageCode.Clean)
            {
                ContinueSending();
            }

            lock (pakInfo)
            {
                switch (pakInfo.State)
                {
                    case PackageState.Empty:
                        _logger.LogTrace($"{this} pak {pakIndex} has been cleaned");
                        return;

                    case PackageState.Sended:
                        _logger.LogTrace($"{this} pak {pakIndex} has been sended. (answer pak repeat)");
                        return;

                    default:
                        pakInfo.State = PackageState.Sended;
                        pakInfo.Package = null;

                        _toRepeatSendPakIDs.Remove(pakIndex);
                        return;
                }
            }
        }

        private IResult SendPayloadPackages(IEnumerable<IPackage> packages)
        {
            lock (this)
            {
                if (_toDistributeIndexPaks.Count + packages.Count() > _options.MaxPackageBuffer)
                {
                    return Result.Create((int)ExchangeCode.SentBufferFull, "发送区缓存已满");
                }

                foreach (var package in packages)
                {
                    _toDistributeIndexPaks.Enqueue(package as PackageWithPayload);
                }
            }

            mre_MorePaksToDistrubute.Set();

            return Result.CreateSuccess();
        }

        private void DistributeIndexTask()
        {
            _logger.LogTrace($"{this} distribute index thread start to run");

            while (_state == ProtocolState.Online)
            {
                PackageWithPayload toDistributeIndexPak = null;

                lock (this)
                {
                    if (_toDistributeIndexPaks.Count > 0)
                    {
                        toDistributeIndexPak = _toDistributeIndexPaks.Dequeue();
                    }
                }

                if (toDistributeIndexPak == null)
                {
                    mre_MorePaksToDistrubute.WaitOne();
                }
                else
                {
                    toDistributeIndexPak.Index = _currIndex;

                    AddToSendDicAndSend(toDistributeIndexPak);

                    _currIndex = (_currIndex + 1) % _maxSendIndex;
                }

                if (_currIndex % _options.MaxPackageBuffer == 0)
                {
                    Clean();

                    mre_ContinueSending.WaitOne();
                }
            }

            _logger.LogTrace($"{this} distribute index thread stop");
        }

        private Task Clean()
        {
            return Task.Run(() =>
            {
                var cleanCount = 0;

                SendCleanPak(_currIndex);

                _currIndex++;
                var toCleanIndex = _currIndex;

                do
                {
                    var pakInfo = _sendingPaks[toCleanIndex];

                    lock (pakInfo)
                    {
                        pakInfo.State = PackageState.Empty;

                        _toRepeatSendPakIDs.Remove(toCleanIndex);
                    }

                    toCleanIndex = (toCleanIndex + 1) % _maxSendIndex;
                    cleanCount++;
                } while (cleanCount < _options.MaxPackageBuffer);
            });
        }

        private void AddToSendDicAndSend(IPackageWithIndex pak)
        {
            _logger.LogTrace($"{this} send pak {pak.Index}");

            var pakInfo = _sendingPaks[pak.Index];

            lock (pakInfo)
            {
                pakInfo.State = PackageState.Sending;
                pakInfo.Package = pak;
            }

            SendPackage(pak);
        }

        private void RepeatSendPaks(object sender, ElapsedEventArgs e)
        {
            timer_RepeatSendPaks.Stop();

            var ids = _toRepeatSendPakIDs.ToArray();

            foreach (var id in ids)
            {
                var pakInfo = _sendingPaks[id];

                if (pakInfo.State == PackageState.ToSend
                    || pakInfo.State == PackageState.Sending)
                {
                    _logger.LogTrace($"{this} repeat send pak {id}");

                    SendPackage(pakInfo.Package);
                }
                else
                {
                    _toRepeatSendPakIDs.Remove(id);
                }
            }

            if (_state == ProtocolState.Online)
            {
                timer_RepeatSendPaks.Start();
            }
        }

        private Task SendCleanPak(int pakIndex)
        {
            return Task.Run(() =>
            {
                _logger.LogTrace($"{this} send clean pak");

                var pak = new PackageWithIndex(PackageCode.Clean)
                {
                    Index = pakIndex
                };

                AddToSendDicAndSend(pak);
            });
        }
    }
}
