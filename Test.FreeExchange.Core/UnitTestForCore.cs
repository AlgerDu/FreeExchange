using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test.FreeExchange.Core
{
    [TestClass]
    public class UnitTestForCore
    {
        readonly IContainer container;

        public UnitTestForCore()
        {
            var builder = new ContainerBuilder();

            builder.AddLogging();
            builder.AddFreeExchangeCore();

            container = builder.Build();
        }

        [TestMethod]
        public void TestMethod1()
        {
        }
    }

    /// <summary>
    /// TODO 迁移到 Core 中，这里相当于是在使用的过程中进行完善
    /// </summary>
    public static class ActofacExtensions
    {
        public static void AddLogging(this ContainerBuilder builder)
        {
            var service = new ServiceCollection();
            service.AddLogging();
            service.AddOptions();

            builder.Populate(service);
        }

        public static void AddFreeExchangeCore(this ContainerBuilder builder)
        {

        }
    }
}
