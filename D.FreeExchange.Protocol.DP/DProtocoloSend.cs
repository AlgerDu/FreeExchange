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

namespace D.FreeExchange.Protocol.DP
{
    /// <summary>
    /// 发送数据的部分
    /// </summary>
    public class DProtocoloSend : IProtocolSend
    {
        ILogger _logger;
        IProtocolCore _core;
        bool _isSending;

        ManualResetEvent mre_MorePaksToDistrubute;
        ManualResetEvent mre_ContinueSending;

        Queue<PackageWithPayload> _toDistributeIndexPaks;
        Dictionary<int, PackageCacheItem> _sendingPaks;
        HashSet<int> _toRepeatSendPakIDs;

        int _currIndex;
        int _maxSendIndex;

        System.Timers.Timer timer_RepeatSendPaks;

        public DProtocoloSend(
            ILogger logger
            , IProtocolCore core
            )
        {
            _logger = logger;
            _core = core;

            _core.OptionsChanged += new ProtocolOptionsChangedEventHandler(OnOptionsChanged);
            _core.StateChanged += new ProtocolStateChangedEventHandler(OnStateChanged);


            mre_MorePaksToDistrubute = new ManualResetEvent(true);
            mre_ContinueSending = new ManualResetEvent(true);

            timer_RepeatSendPaks = new System.Timers.Timer();
            timer_RepeatSendPaks.Elapsed += new System.Timers.ElapsedEventHandler(RepeatSendPaks);

            _toRepeatSendPakIDs = new HashSet<int>();

            _isSending = false;
        }

        public override string ToString()
        {
            return $"{_core} send";
        }

        #region IProtocolSend 接口实现
        public IResult DistributeThenSendPackages(IEnumerable<IPackage> packages)
        {
            return SendPayloadPackages(packages);
        }

        public void DealAnswer(IPackage package)
        {
            var index = (package as IPackageWithIndex).Index;
            ReceivedIndexPak(index);
        }
        #endregion

        private void OnStateChanged(object sender, ProtocolStateChangedEventArgs e)
        {
            if (e.NewState == ProtocolState.Closing || e.NewState == ProtocolState.Stop)
            {
                //停止并且清理
                StopSending();
                Clear();
            }
            else if (e.OldState == ProtocolState.Connectting && e.NewState == ProtocolState.Online)
            {
                //启动发送
                StartSending();
            }
            else if (e.OldState == ProtocolState.Online && e.NewState == ProtocolState.Offline)
            {
                //停止发送
                StopSending();
            }
        }

        private void OnOptionsChanged(object sender, ProtocolOptionsChangedEventArgs e)
        {
            //清理之后，
            //重新设置相关缓存的大小
            Clear();

            var maxPakBuffer = e.Options.MaxPackageBuffer;

            _maxSendIndex = maxPakBuffer * 4;
            _toDistributeIndexPaks = new Queue<PackageWithPayload>(maxPakBuffer);
            _sendingPaks = new Dictionary<int, PackageCacheItem>(_maxSendIndex);

            timer_RepeatSendPaks.Interval = e.Options.PaylodPakRepeatSendInterval;
        }

        /// <summary>
        /// 开始发送
        /// </summary>
        private void StartSending()
        {
            lock (this)
            {
                _isSending = true;

                timer_RepeatSendPaks.Start();

                Task.Run(() => DistributeIndexTask());
            }
        }

        /// <summary>
        /// 停止发送
        /// </summary>
        private void StopSending()
        {
            timer_RepeatSendPaks.Stop();

            mre_ContinueSending.Set();
            mre_MorePaksToDistrubute.Set();
        }

        /// <summary>
        /// 清理缓存中的东西
        /// </summary>
        private void Clear()
        {
            lock (this)
            {
                _currIndex = 0;

                _toRepeatSendPakIDs?.Clear();
                _toDistributeIndexPaks?.Clear();
                _sendingPaks?.Clear();
            }
        }

        /// <summary>
        /// index 分配线程，按照顺序，给队列中的包分配 index
        /// </summary>
        private void DistributeIndexTask()
        {
            _logger.LogTrace($"{this} distribute index thread start to run");

            while (_isSending)
            {
                //先清理，这样保证第一次运行的时候，就会清理，防止连接上了已经在运行的
                if (_currIndex % _core.Options.MaxPackageBuffer == 0)
                {
                    Clean();

                    mre_ContinueSending.WaitOne();
                }

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
            }

            _logger.LogTrace($"{this} distribute index thread stop");
        }

        /// <summary>
        /// 将需要发送的包添加到字典中，并且第一次发送出去
        /// </summary>
        /// <param name="pak"></param>
        private void AddToSendDicAndSend(IPackageWithIndex pak)
        {
            _logger.LogTrace($"{this} send pak {pak.Index}");

            var pakInfo = _sendingPaks[pak.Index];

            lock (pakInfo)
            {
                pakInfo.State = PackageState.Sending;
                pakInfo.Package = pak;
            }

            _core.SendPackage(pak);
        }

        /// <summary>
        /// 发送清理包
        /// </summary>
        /// <param name="pakIndex"></param>
        private async void SendCleanPak(int pakIndex)
        {
            await Task.Run(() =>
            {
                _logger.LogTrace($"{this} send clean pak");

                var pak = new PackageWithIndex(PackageCode.Clean)
                {
                    Index = pakIndex
                };

                AddToSendDicAndSend(pak);
            });
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
                if (_toDistributeIndexPaks.Count + packages.Count() > _core.Options.MaxPackageBuffer)
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
                } while (cleanCount < _core.Options.MaxPackageBuffer);
            });
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

                    _core.SendPackage(pakInfo.Package);
                }
                else
                {
                    _toRepeatSendPakIDs.Remove(id);
                }
            }

            if (_isSending)
            {
                timer_RepeatSendPaks.Start();
            }
        }
    }
}
