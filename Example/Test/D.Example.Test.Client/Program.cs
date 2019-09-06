using D.Infrastructures;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using System;
using System.IO;

namespace D.Example.Test.Client
{
    class Program
    {
        static void Main(string[] args)
        {
            var app = new ApplicationBuilder()
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.SetBasePath(Directory.GetCurrentDirectory());
                    config.AddJsonFile("appSettings.json", optional: false, reloadOnChange: true);
                })
                .ConfigureLogging(logging =>
                {
                    logging.AddConsole();
                    logging.AddNLog();
                })
                .UseStartupWithAutofac<Startup>()
                .Builde<TestClientApp>();

            app.Run();
            Console.ReadKey();

            app.Stop();
            Console.ReadKey();
        }
    }
}
