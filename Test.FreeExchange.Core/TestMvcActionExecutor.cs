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
    public enum TestEnum
    {
        One,
        Two,
        Three
    }

    public class TestController : IExchangeController
    {
        public IExchangeProxy Proxy { get; private set; }

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

        public int Sum(int a, int b)
        {
            return a + b;
        }

        public TestEnum EnumForward(TestEnum test)
        {
            return (TestEnum)(((int)test + 1) % 3);
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

            var rst = _executor.InvokeAction(msg);

            Assert.AreEqual(rst.Data, "test");
        }

        [TestMethod]
        public void TestSumAction()
        {
            var a = 3;
            var b = 4;

            var msg = CreateMsg("test/sum", a, b);
            var rst = _executor.InvokeAction(msg);

            Assert.AreEqual(rst.Data, a + b);
        }

        [TestMethod]
        public void TestEnumAction()
        {
            var msg = CreateMsg("test/enumForward", TestEnum.Three);
            var rst = _executor.InvokeAction(msg);

            Assert.AreEqual(rst.Data, TestEnum.One);
        }

        private IActionExecuteMessage CreateMsg(string url, params object[] requestParams)
        {
            for (var i = 0; i < requestParams.Length; i++)
            {
                requestParams[i] = JsonConvert.SerializeObject(requestParams[i]);
            }

            return new ActionExecuteMessage
            {
                Url = url,
                Params = requestParams
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
