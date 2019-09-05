using Autofac;
using D.FreeExchange.Core;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace D.FreeExchange
{
    public static class Extensions_Autofac_FreeExchangeCore
    {
        public static void AddFreeExchangeCore(this ContainerBuilder builder)
        {
            builder.RegisterType<UdpExchangeServer>()
                .As<IExchangeServer>()
                .AsSelf();

            builder.RegisterType<ExchangeClientProxy>()
                .As<IExchangeClientProxy>()
                .AsSelf();

            builder.RegisterType<UdpExchangeServerProxy>()
                .As<IExchangeServerProxy>()
                .AsSelf();

            builder.RegisterType<MvcActionExecutor>()
                .As<IActionExecutor>()
                .AsSelf();

            builder.RegisterType<UdpClientProxyTransporter>()
                .As<ITransporter>()
                .AsSelf();

            builder.RegisterType<UdpServerProxyTransporter>()
                .As<ITransporter>()
                .AsSelf();

            builder.RegisterType<DProtocol>()
                .As<DProtocol>()
                .AsSelf();
        }

        public static UdpServerProxyTransporter ResolveUdpServerProxyTransporter(
            this IComponentContext context
            , string address)
        {
            return context.Resolve<UdpServerProxyTransporter>(
                new TypedParameter(typeof(string), address)
                );
        }

        public static UdpClientProxyTransporter ResolveUdpClientProxyTransporter(
            this IComponentContext context
            , IPEndPoint endPoint
            , UdpClient client)
        {
            return context.Resolve<UdpClientProxyTransporter>(
                new TypedParameter(typeof(IPEndPoint), endPoint)
                , new TypedParameter(typeof(UdpClient), client)
                );
        }

        public static DProtocol ResolveDProtocol(
            this IComponentContext context
            , ExchangeProtocolRunningMode mode
            )
        {
            return context.Resolve<DProtocol>(
                new TypedParameter(typeof(ExchangeProtocolRunningMode), mode)
                );
        }
    }
}
