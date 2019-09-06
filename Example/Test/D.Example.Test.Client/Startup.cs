using Autofac;
using D.FreeExchange;
using D.Infrastructures;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace D.Example.Test
{
    public class Startup
    {
        IHostingEnvironment _env;
        IConfiguration _config;
        ILoggerFactory _loggerFactory;

        public Startup(
            IHostingEnvironment env
            , IConfiguration config
            , ILoggerFactory loggerFactory
            )
        {
            _env = env;
            _config = config;
            _loggerFactory = loggerFactory;
        }

        public void ConfigOptions(IServiceCollection services)
        {
            services.Configure<ServerProxyOptions>(_config.GetSection("Server"));

            services.Configure<DProtocolOptions>((options) =>
            {
                options.MaxPackageBuffer = 64;
                options.MaxPayloadDataLength = 2048;
            });
        }

        public void ConfigServices(IServiceCollection services)
        {
            services.AddSingleton<ServerProxy>();
        }

        public void ConfigServices(ContainerBuilder builder)
        {
            builder.AddFreeExchangeCore();
        }
    }
}
