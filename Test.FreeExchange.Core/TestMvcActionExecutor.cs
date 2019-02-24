using Autofac;
using D.FreeExchange;
using D.FreeExchange.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Test.FreeExchange.Core
{
    public class TestController : IExchangeController
    {
        public IExchangeClientProxy Client { get; private set; }

        ILogger _logger;

        public TestController(
            ILogger<TestController> logger
            )
        {
            _logger = logger;
        }

        public string Value(string v)
        {
            return v;
        }
    }

    [TestClass]
    public class TestMvcActionExecutor
    {
        readonly IContainer _container;
        readonly IActionExecutor _executor;

        public TestMvcActionExecutor()
        {
            _container = CreateContainer();
            _executor = _container.Resolve<IActionExecutor>();
        }

        [TestMethod]
        public void TestCreateMvcActionExecutor()
        {
            var executor = _container.Resolve<IActionExecutor>();

            Assert.AreNotEqual(executor, null);
        }

        [TestMethod]
        public void TestSimpleAction()
        {
            var msg = CreateMsg("test/value", "test");

            var rst = _executor.InvokeAction(msg, null);

            Assert.AreEqual(rst.Data, "test");
        }

        private IExchangeMessage CreateMsg(string url, params object[] requestParams)
        {
            var jsonStr = JsonConvert.SerializeObject(requestParams);

            return new ExchangeMessage
            {
                Url = url,
                Params = new object[] { jsonStr }
            };
        }

        /// <summary>
        /// 构建容器
        /// </summary>
        /// <returns></returns>
        private IContainer CreateContainer()
        {
            var builder = new ContainerBuilder();

            builder.AddMicrosoftExtensions();

            builder.RegisterType<MvcActionExecutor>()
                .As<IActionExecutor>()
                .SingleInstance();

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

            //builder.RegisterInstance<MvcActionExecutorOptions>(options);

            return builder.Build();
        }
    }
}
