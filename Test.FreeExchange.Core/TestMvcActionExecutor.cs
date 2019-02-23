using Autofac;
using D.FreeExchange;
using D.FreeExchange.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Test.FreeExchange.Core
{
    [TestClass]
    public class TestMvcActionExecutor
    {
        readonly IContainer container;

        public TestMvcActionExecutor()
        {
            container = CreateContainer();
        }

        [TestMethod]
        public void TestCreateMvcActionExecutor()
        {
            var executor = container.Resolve<IActionExecutor>();

            Assert.AreNotEqual(executor, null);
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

            var options = new MvcActionExecutorOptions
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

            builder.RegisterInstance<MvcActionExecutorOptions>(options);

            return builder.Build();
        }
    }
}
