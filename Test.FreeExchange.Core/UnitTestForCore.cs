using Autofac;
using Autofac.Extensions.DependencyInjection;
using D.FreeExchange;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test.FreeExchange.Core
{
    [TestClass]
    public class UnitTestForCore
    {
        readonly IContainer container;

        IExchangeServer _server;

        public UnitTestForCore()
        {
            container = CreateContainer();

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

            return builder.Build();
        }

        /// <summary>
        /// 启动测试用 UdpServer
        /// </summary>
        private void RunTestServer()
        {
            _server = container.Resolve<IExchangeServer>();

            _server.Run();
        }

        [TestMethod]
        public void TestMethod1()
        {
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
