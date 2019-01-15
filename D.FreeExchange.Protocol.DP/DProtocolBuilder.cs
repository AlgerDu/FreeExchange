using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using D.Utils;
using Microsoft.Extensions.Logging;

namespace D.FreeExchange
{
    public class DProtocolBuilder : IProtocolBuilder
    {
        readonly Encoding _encoding = Encoding.ASCII;

        ILogger _logger;

        ITransporter _transporter;

        public DProtocolBuilder(
            ILogger<DProtocolBuilder> logger
            , ITransporter transporter
            )
        {
            _logger = logger;
            _transporter = transporter;
        }

        #region IProtocolBuilder 实现
        public IProtocolBuilder Run()
        {
            throw new NotImplementedException();
        }

        public Task<IResult> SendAsync(IProtocolPayload payload)
        {
            return Task.Run(() => AddToSendQueueAndTryStartLoop(payload));
        }

        public void SetControlReceiveAction(Action<int> action)
        {
            throw new NotImplementedException();
        }

        public void SetPayloadReceiveAction(Action<IProtocolPayload> action)
        {
            throw new NotImplementedException();
        }

        public IProtocolBuilder Stop()
        {
            throw new NotImplementedException();
        }
        #endregion

        #region 发送，暂时先都写在这里，写完在考虑封装整理

        const int _maxMarkIndex = 1024;

        int _sendMarkOffest = 0;
        int _sendIndex = 0;
        byte[] _sendMark = new byte[_maxMarkIndex];
        bool _sendLoopIsRunning = false;
        object _sendLock = new object();

        Queue<IProtocolPayload> _toSendPayloads = new Queue<IProtocolPayload>();

        private IResult AddToSendQueueAndTryStartLoop(IProtocolPayload payload)
        {
            lock (_sendLock)
            {
                _toSendPayloads.Enqueue(payload);

                if (!_sendLoopIsRunning)
                {
                    _sendLoopIsRunning = true;
                    Task.Run(() => StartLoop());
                }

                return Result.CreateSuccess();
            }
        }

        private void StartLoop()
        {
            IProtocolPayload payload;

            lock (_sendLock)
            {
                if (_toSendPayloads.Count == 0)
                {
                    payload = null;
                    _sendLoopIsRunning = false;
                }
                else
                {
                    payload = _toSendPayloads.Dequeue();
                }
            }

            while (payload != null)
            {
                AnalysePayload(payload);

                lock (_sendLock)
                {
                    if (_toSendPayloads.Count == 0)
                    {
                        payload = null;
                        _sendLoopIsRunning = false;
                    }
                    else
                    {
                        payload = _toSendPayloads.Dequeue();
                    }
                }
            }
        }

        private void AnalysePayload(IProtocolPayload payload)
        {

        }

        #endregion

        #region 接收，和发送一样

        #endregion
    }
}
