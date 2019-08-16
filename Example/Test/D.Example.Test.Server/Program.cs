using D.Infrastructures;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;

namespace D.Example.Test
{
    class Program
    {
        static void Main(string[] args)
        {
            var app = new ApplicationBuilder()
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.SetBasePath(Directory.GetCurrentDirectory());
                    //config.AddJsonFile("appSettings.json", optional: false, reloadOnChange: true);
                })
                .ConfigureLogging(logging =>
                {
                    //logging.AddConsole();
                })
                .UseStartupWithAutofac<Startup>()
                .Builde<TestServerApp>();

            app.Run();
            Console.ReadKey();

            app.Stop();
            Console.ReadKey();
        }
    }
}
