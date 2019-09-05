using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using D.Utils;
using Microsoft.Extensions.Logging;

namespace D.FreeExchange.Core
{
    public class ClientCache
    {
        public ExchangeClientProxy Proxy { get; set; }

        public UdpClientProxyTransporter Transporter { get; set; }
    }

    public class UdpExchangeServer
        : IExchangeServer
    {
        ILogger<UdpExchangeServer> _logger;
        ILifetimeScope _scope;

        int _listenPort;

        Dictionary<string, ClientCache> _clientProxies;
        UdpClient _server;

        /// <summary>
        /// TOTO 构造函数应该注入一个 options 感觉这样比较好
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="listenPort"></param>
        /// <param name="scope"></param>
        public UdpExchangeServer(
            ILogger<UdpExchangeServer> logger
            , int listenPort
            , ILifetimeScope scope
            )
        {
            _logger = logger;
            _listenPort = listenPort;

            _scope = scope;

            _clientProxies = new Dictionary<string, ClientCache>();
        }

        #region IExchangeServer 实现

        public IEnumerable<IExchangeClientProxy> ClientProxies => _clientProxies.Values.Select(cc => cc.Proxy);

        public Task<IResult> Run()
        {
            return Task.Run<IResult>(() =>
            {
                return StartServer();
            });
        }

        public IExchangeClientProxy FindByUid(Guid uid)
        {
            return _clientProxies
                .Values
                .Where(pp => pp.Proxy.Uid == uid)
                .FirstOrDefault()
                ?.Proxy;
        }

        #endregion 

        private IResult StartServer()
        {
            IPEndPoint ipep = new IPEndPoint(IPAddress.Any, _listenPort);
            _server = new UdpClient(ipep);
            _server.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

            _server.BeginReceive(ServiceReceivedData, _server);

            return Result.CreateSuccess();
        }

        private void ServiceReceivedData(IAsyncResult ar)
        {
            var client = ar.AsyncState as UdpClient;

            var endpoint = new IPEndPoint(IPAddress.Any, 0);
            var buffer = client.EndReceive(ar, ref endpoint);

            DealBuffer(buffer, endpoint);

            client.BeginReceive(ServiceReceivedData, client);
        }

        private async void DealBuffer(byte[] buffer, IPEndPoint endPoint)
        {
            //看看这个 end point 所对应的客户端还在不在，
            //在的话继续处理；
            //不在了要重新生成一个客户端
            var cache = FindClientProxyByEndpoint(endPoint);

            if (cache == null)
            {
                cache = CreateClientProxy(endPoint);
            }

            await cache.Transporter.ServerReceiveBuffer(buffer, 0, buffer.Length);
        }

        private ClientCache CreateClientProxy(IPEndPoint endPoint)
        {
            var transpoter = _scope.ResolveUdpClientProxyTransporter(endPoint, _server);

            var protocol = _scope.ResolveDProtocol(ExchangeProtocolRunningMode.Server);

            var client = _scope.Resolve<ExchangeClientProxy>(
                new TypedParameter(typeof(IPEndPoint), endPoint)
                , new TypedParameter(typeof(ITransporter), transpoter)
                , new TypedParameter(typeof(IExchangeProtocol), transpoter)
                );

            client.Run();

            var cache = new ClientCache
            {
                Proxy = client,
                Transporter = transpoter
            };

            _clientProxies.Add(endPoint.ToString(), cache);

            return cache;
        }

        private ClientCache FindClientProxyByEndpoint(IPEndPoint endPoint)
        {
            if (_clientProxies.ContainsKey(endPoint.ToString()))
            {
                return _clientProxies[endPoint.ToString()];
            }
            else
            {
                return null;
            }
        }
    }
}
