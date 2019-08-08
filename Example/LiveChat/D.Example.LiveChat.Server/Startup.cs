using Autofac;
using D.Infrastructures;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace D.Example.LiveChat.Server
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
        }

        public void ConfigServices(IServiceCollection services)
        {
        }

        public void ConfigServices(ContainerBuilder builder)
        {
        }
    }
}
