using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using D.Utils;
using Microsoft.Extensions.Logging;

namespace D.FreeExchange
{
    /// <summary>
    /// Upd Transporter
    /// </summary>
    public class UdpTransporter : ITransporter
    {
        ILogger _logger;

        UdpClient _client;
        IPEndPoint _sender;
        string _address;

        Action<byte[], int, int> _receiveBufferAction;

        public IPEndPoint Sender => _sender;

        public string Address => _address;

        public UdpTransporter(
            ILogger<UdpTransporter> logger
            , UdpClient client
            , IPEndPoint sender
            )
        {
            _logger = logger;

            _client = client;
            _sender = sender;

            _address = sender.ToString();
        }

        /// <summary>
        /// 暂时性的尝试
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="index"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public Task ServerReceiveBuffer(byte[] buffer, int index, int length)
        {
            return Task.Run(() =>
            {
                _logger.LogTrace($"{this} 收到了 {length} 个 byte 的数据");

                if (_receiveBufferAction == null)
                {
                    _logger.LogWarning($"UdpTransporter ReceiveBufferAction is null");
                }

                _receiveBufferAction?.Invoke(buffer, index, length);
            });
        }

        #region ITransporter 实现
        public Task<IResult> Close()
        {
            return Task.Run<IResult>(() =>
            {
                return Result.CreateSuccess();
            });
        }

        public Task<IResult> Connect()
        {
            return Task.Run<IResult>(() =>
            {
                return Result.CreateSuccess();
            });
        }

        public Task<IResult> SendAsync(byte[] buffer, int index, int length)
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

        public void SetReceiveAction(Action<byte[], int, int> action)
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
