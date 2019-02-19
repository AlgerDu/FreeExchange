using D.Utils;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace D.FreeExchange
{
    /// <summary>
    /// Upd Transporter
    /// </summary>
    public abstract class TemplateUdpTransporter : ITransporter
    {
        protected ILogger _logger;

        protected UdpClient _client;
        protected IPEndPoint _sender;
        protected string _address;

        protected Action<byte[], int, int> _receiveBufferAction;

        public virtual IPEndPoint Sender => _sender;

        public virtual string Address => _address;

        public TemplateUdpTransporter(
            ILogger logger
            )
        {
            _logger = logger;
        }

        #region ITransporter 实现
        public virtual Task<IResult> Close()
        {
            return Task.Run<IResult>(() =>
            {
                return Result.CreateSuccess();
            });
        }

        public virtual Task<IResult> Connect()
        {
            return Task.Run<IResult>(() =>
            {
                _client?.Close();
                return Result.CreateSuccess();
            });
        }

        public virtual Task<IResult> SendAsync(byte[] buffer, int index, int length)
        {
            return Task.Run<IResult>(() =>
            {
                try
                {
                    var toSend = buffer;

                    if (index != 0)
                    {
                        throw new Exception("暂时不支持 index > 0 的情况");
                    }

                    var sendByteNum = _client.Send(toSend, length, _sender);

                    if (sendByteNum != length)
                    {
                        _logger.LogWarning($"{this} 需要发送 {length} 个字节，但是只发送了 {sendByteNum} 个");
                    }

                    return Result.CreateSuccess();
                }
                catch (Exception ex)
                {
                    _logger.LogError($"{this} 发送数据的过程中出现异常:{ex}");

                    return Result.CreateError();
                }
            });
        }

        public virtual void SetReceiveAction(Action<byte[], int, int> action)
        {
            _receiveBufferAction = action;
        }
        #endregion

        public override string ToString()
        {
            return $"Udp[{_sender?.ToString()}]";
        }
    }
}
