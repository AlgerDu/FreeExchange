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

        private void AnylseAddress()
        {
            var arr = _address.Split(':');
            var ipstr = arr[0];
            var port = Convert.ToInt32(arr[1]);

            var ipa = IPAddress.Parse(ipstr);
            _sender = new IPEndPoint(ipa, port);

            _client = new UdpClient();
        }

        private void ReceivedData(IAsyncResult ar)
        {
            var client = ar.AsyncState as UdpClient;

            var endpoint = new IPEndPoint(IPAddress.Any, 0);
            var buffer = client.EndReceive(ar, ref endpoint);

            DealBuffer(buffer, endpoint);

            client.BeginReceive(ReceivedData, client);
        }

        private async void DealBuffer(byte[] buffer, IPEndPoint endPoint)
        {
            await Task.Run(() =>
            {
                _receiveBufferAction?.Invoke(buffer, 0, buffer.Length);
            });
        }
    }
}
