using Autofac;
using Autofac.Extensions.DependencyInjection;
using D.FreeExchange;
using D.FreeExchange.Core;
using D.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Reflection;

namespace Test.FreeExchange.Core
{
    public class TestCoreController : IExchangeController
    {
        public IExchangeProxy Proxy { get; private set; }

        ILogger _logger;

        public TestCoreController(
            ILogger<TestCoreController> logger
            )
        {
            _logger = logger;
        }

        public IResult<int> Sum(int a, int b)
        {
            return Result.CreateSuccess<int>(a + b);
        }
    }

    [TestClass]
    public class TestCore
    {
        readonly IContainer _container;

        IExchangeServer _server;

        public TestCore()
        {
            _container = CreateContainer();

            RunTestServer();
        }

        /// <summary>
        /// 构建容器
        /// </summary>
        /// <returns></returns>
        private IContainer CreateContainer()
        {
            var builder = new ContainerBuilder();

            builder.AddMicrosoftExtensions();
            builder.AddFreeExchangeCore();

            var optionsTmp = new MvcActionExecutorOptions
            {
                AssemblyNames = new string[]
                {
                    Assembly.GetAssembly(this.GetType()).FullName
                },
                Namespaces = new string[]
                {
                    "Test.FreeExchange.Core"
                }
            };

            builder.RegisterInstance<IOptions<MvcActionExecutorOptions>>(Options.Create<MvcActionExecutorOptions>(optionsTmp));


            return builder.Build();
        }

        /// <summary>
        /// 启动测试用 UdpServer
        /// </summary>
        private void RunTestServer()
        {
            _server = _container.Resolve<IExchangeServer>(
                new TypedParameter(typeof(int), 8066)
                );

            _server.Run();
        }

        [TestMethod]
        public void TestConnectToServer()
        {
            var transporter = _container.ResolveUdpServerProxyTransporter("127.0.0.1:8066");

            var serverProxy = _container.Resolve<UdpExchangeServerProxy>();
            serverProxy.UpdateTransporter(transporter);
            serverProxy.Connect().Wait();

            var t = serverProxy.SendAsync<Result<int>>(new ExchangeMessage
            {
                Url = "testcore/sum",
                Params = new object[] { 4, 5 },
                Timeout = TimeSpan.FromSeconds(20)
            });

            t.Wait();


            Assert.AreEqual(t.Result.Data, 9);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public static class ActofacExtensions
    {
        public static void AddMicrosoftExtensions(this ContainerBuilder builder)
        {
            var service = new ServiceCollection();
            service.AddLogging();
            service.AddOptions();

            builder.Populate(service);
        }
    }
}
