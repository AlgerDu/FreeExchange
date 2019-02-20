using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace D.FreeExchange
{
    public class UdpClientProxyTransporter :
        TemplateUdpTransporter
        , ITransporter
    {
        public UdpClientProxyTransporter(
            ILogger<UdpClientProxyTransporter> logger
            , UdpClient client
            , IPEndPoint sender
            ) : base(logger)
        {
            _client = client;
            _sender = sender;

            _address = _sender.ToString();
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
    }
}
