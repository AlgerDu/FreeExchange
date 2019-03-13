using Autofac;
using D.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using D.FreeExchange;

namespace Test.DProtocolBuilder
{
    [TestClass]
    public class Test_DP
    {


        private IContainer CreateContainer()
        {
            var builder = new ContainerBuilder();

            builder.AddMicrosoftExtensions();

            return builder.Build();
        }
    }

    public class TestTaransporterForDP : ITransporter
    {

    }
}
