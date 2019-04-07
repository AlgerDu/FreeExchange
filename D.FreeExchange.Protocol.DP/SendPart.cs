using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using D.Utils;
using Microsoft.Extensions.Logging;

namespace D.FreeExchange.Protocol.DP
{
    internal class SendPart : ISendPart
    {
        ILogger _logger;
        IShareData _shareData;

        Action<byte[], int, int> _sendBufferAction;

        ManualResetEvent mre_MorePaksToDistrubute;
        ManualResetEvent mre_ContinueSending;
        Queue<PackageWithPayload> _toDistributeIndexPaks;

        HashSet<int> _toRepeatSendPakIDs;

        int _currIndex;
        int _maxSendIndex;

        System.Timers.Timer timer_RepeatSendPaks;

        public SendPart(
            ILogger logger
            , IShareData shareData
            )
        {
            _logger = logger;
            _shareData = shareData;

            _toRepeatSendPakIDs = new HashSet<int>();

            mre_MorePaksToDistrubute = new ManualResetEvent(true);
            mre_ContinueSending = new ManualResetEvent(true);

            timer_RepeatSendPaks = new System.Timers.Timer();
            timer_RepeatSendPaks.Elapsed += new System.Timers.ElapsedEventHandler(RepeatSendPaks);
        }

        public void Run()
        {
            var maxPakBuffer = _shareData.Options.MaxPackageBuffer;

            _currIndex = 0;
            _maxSendIndex = maxPakBuffer * 4;
            _toDistributeIndexPaks = new Queue<PackageWithPayload>(maxPakBuffer);

            timer_RepeatSendPaks.Interval = _shareData.Options.PaylodPakRepeatSendInterval;
            timer_RepeatSendPaks.Start();

            Task.Run(() => DistributeIndexTask());
        }

        public void Stop()
        {
            mre_MorePaksToDistrubute.Set();
            mre_ContinueSending.Set();
        }

        public void ContinueSending()
        {
            mre_ContinueSending.Set();
        }

        public void ReceivedIndexPak(int pakIndex)
        {
            var pakInfo = _shareData.SendingPaks[pakIndex];

            lock (pakInfo)
            {
                switch (pakInfo.State)
                {
                    case PackageState.Empty:
                        _logger.LogTrace($"{_shareData.Uid} pak {pakIndex} has been cleaned");
                        return;

                    case PackageState.Sended:
                        _logger.LogTrace($"{_shareData.Uid} pak {pakIndex} has been sended. (answer pak repeat)");
                        return;

                    default:
                        pakInfo.State = PackageState.Sended;
                        pakInfo.Package = null;

                        _toRepeatSendPakIDs.Remove(pakIndex);
                        return;
                }
            }
        }

        public IResult SendPayloadPackages(IEnumerable<IPackage> packages)
        {
            lock (this)
            {
                if (_toDistributeIndexPaks.Count + packages.Count() > _shareData.Options.MaxPackageBuffer)
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

        public void SetSendBufferAction(Action<byte[], int, int> action)
        {
            _sendBufferAction = action;
        }

        private void DistributeIndexTask()
        {
            _logger.LogTrace($"{_shareData.Uid} distribute index thread start to run");

            while (_shareData.BuilderIsRunning)
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

                if (_currIndex % _shareData.Options.MaxPackageBuffer == 0)
                {
                    Clean();

                    mre_ContinueSending.WaitOne();
                }
            }

            _logger.LogTrace($"{_shareData.Uid} distribute index thread stop");
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
                    var pakInfo = _shareData.SendingPaks[toCleanIndex];

                    lock (pakInfo)
                    {
                        pakInfo.State = PackageState.Empty;

                        _toRepeatSendPakIDs.Remove(toCleanIndex);
                    }

                    toCleanIndex = (toCleanIndex + 1) % _maxSendIndex;
                    cleanCount++;
                } while (cleanCount < _shareData.Options.MaxPackageBuffer);
            });
        }

        private void AddToSendDicAndSend(IPackageWithIndex pak)
        {
            _logger.LogTrace($"{_shareData.Uid} send pak {pak.Index}");

            var pakInfo = _shareData.SendingPaks[pak.Index];

            lock (pakInfo)
            {
                pakInfo.State = PackageState.Sending;
                pakInfo.Package = pak;
            }

            SendPackage(pak);
        }

        private Task SendPackage(IPackage package)
        {
            return Task.Run(() =>
            {
                if (_sendBufferAction != null)
                {
                    var buffer = package.ToBuffer();
                    _sendBufferAction(buffer, 0, buffer.Length);
                }
                else
                {
                    _logger.LogWarning($"{_shareData.Uid} SendBufferAction is null");
                }
            });
        }

        private void RepeatSendPaks(object sender, ElapsedEventArgs e)
        {
            timer_RepeatSendPaks.Stop();

            var ids = _toRepeatSendPakIDs.ToArray();

            foreach (var id in ids)
            {
                var pakInfo = _shareData.SendingPaks[id];

                if (pakInfo.State == PackageState.ToSend
                    || pakInfo.State == PackageState.Sending)
                {
                    _logger.LogTrace($"{_shareData.Uid} repeat send pak {id}");

                    SendPackage(pakInfo.Package);
                }
                else
                {
                    _toRepeatSendPakIDs.Remove(id);
                }
            }

            if (_shareData.BuilderIsRunning)
            {
                timer_RepeatSendPaks.Start();
            }
        }

        private Task SendCleanPak(int pakIndex)
        {
            return Task.Run(() =>
            {
                _logger.LogTrace($"{_shareData.Uid} send clean pak");

                var pak = new PackageWithIndex(PackageCode.Clean)
                {
                    Index = pakIndex
                };

                AddToSendDicAndSend(pak);
            });
        }
    }
}
