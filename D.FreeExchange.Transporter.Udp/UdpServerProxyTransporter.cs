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
    public class UdpServerProxyTransporter :
        TemplateUdpTransporter
        , ITransporter
    {
        public UdpServerProxyTransporter(
            ILogger<UdpServerProxyTransporter> logger
            , string address
            ) : base(logger)
        {
            _address = address;

            AnylseAddress();
        }

        public override Task<IResult> Connect()
        {
            _client.Connect(_sender);

            _client.BeginReceive(ReceivedData, _client);

            return base.Connect();
        }

        public override Task<IResult> SendAsync(byte[] buffer, int index, int length)
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

                    var sendByteNum = _client.Send(toSend, length);

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

        private void AnylseAddress()
        {
            var arr = _address.Split(':');
            var ipstr = arr[0];
            var port = Convert.ToInt32(arr[1]);

            var ipa = IPAddress.Loopback;

            if (ipstr.ToLower() != "localhost")
            {
                ipa = IPAddress.Parse(ipstr);
            }
            _sender = new IPEndPoint(ipa, port);

            _client = new UdpClient();
            _client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        }

        private void ReceivedData(IAsyncResult ar)
        {
            //var client = ar.AsyncState as UdpClient;

            var buffer = _client.EndReceive(ar, ref _sender);

            DealBuffer(buffer, _sender);

            _client.BeginReceive(ReceivedData, _client);
        }

        private async void DealBuffer(byte[] buffer, IPEndPoint endPoint)
        {
            await Task.Run(() =>
            {
                //_receiveBufferAction?.Invoke(buffer, 0, buffer.Length);
                if (_receiveBufferAction != null)
                {
                    _receiveBufferAction(buffer, 0, buffer.Length);
                }
            });
        }
    }
}
