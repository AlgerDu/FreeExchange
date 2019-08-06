using Autofac;
using Autofac.Extensions.DependencyInjection;
using D.FreeExchange;
using D.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NLog.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Test.DProtocolT
{
    class Program
    {
        static IContainer _container;

        static IExchangeProtocol _client;
        static IExchangeProtocol _server;

        static DProtocolOptions _options;

        static Dictionary<Guid, TaskCompletionSource<IResult<IProtocolPayload>>> _taskCaches;

        static void Main(string[] args)
        {
            _container = CreateContainer();

            _client = _container.Resolve<IExchangeProtocol>();
            _server = _container.Resolve<IExchangeProtocol>();

            _client.SetReceivedCmdAction(ReveivedCtlAction);
            _client.SetReceivedPayloadAction(ReceivedPayloadAction);
            _client.SetSendBufferAction(MockClientSendBuffer);

            _server.SetReceivedCmdAction(ReveivedCtlAction);
            _server.SetReceivedPayloadAction(ReceivedPayloadAction);
            _server.SetSendBufferAction(MockServerSendBuffer);

            _taskCaches = new Dictionary<Guid, TaskCompletionSource<IResult<IProtocolPayload>>>();

            _client.Run(ExchangeProtocolRunningMode.Client);
            _server.Run(ExchangeProtocolRunningMode.Server);

            TestSimpleDP();
        }

        public static void TestSimpleDP()
        {
            var model = new PayloadUid
            {
                Uid = Guid.NewGuid()
            };

            var rst = SenAndReceive(model);
            var payload = rst.Data;

            if (payload.Text == JsonConvert.SerializeObject(model))
            {
                Console.WriteLine("yes");
            }
            else
            {
                Console.WriteLine("no");
            }

            if (payload.Bytes.Count() == 0)
            {
                Console.WriteLine("yes");
            }
            else
            {
                Console.WriteLine("no");
            }
        }

        public void TestDataExceedSendBuffer()
        {
            // DProtocolBuilderOptions.MaxPackageBuffer * MaxPayloadDataLength

            var model = new TxtPayload
            {
                Uid = Guid.NewGuid()
            };

            for (var i = 0; i < _options.MaxPackageBuffer; i++)
            {
                for (var j = 0; j < _options.MaxPayloadDataLength; j++)
                {
                    model.Txt += "a";
                }
            }

            var rst = SenAndReceive(model);
        }

        private static IResult<IProtocolPayload> SenAndReceive<T>(
            T dataWithUid
            , params IByteDescription[] bytes
            )
            where T : IPayloadUid
        {
            var tcs = new TaskCompletionSource<IResult<IProtocolPayload>>();

            lock (_taskCaches)
            {
                _taskCaches.Add(dataWithUid.Uid, tcs);

                Task.Run(() =>
                {
                    var payload = new ProtocolPayload
                    {
                        Text = JsonConvert.SerializeObject(dataWithUid),
                        Bytes = bytes
                    };

                    var task = _client.PushPayload(payload);
                    task.Wait();

                    var rst = task.Result;

                    if (!rst.IsSuccess())
                    {
                        tcs.SetResult(Result.Create<IProtocolPayload>(rst.Code, null, rst.Msg));
                    }
                });
            }

            tcs.Task.Wait();

            return tcs.Task.Result;
        }

        private static IContainer CreateContainer()
        {
            var builder = new ContainerBuilder();
            var service = new ServiceCollection();

            service.AddOptions();

            service.AddLogging(logging =>
            {
                logging.AddNLog();
                logging.AddConsole();
                logging.SetMinimumLevel(LogLevel.Trace);
            });

            _options = new DProtocolOptions();

            service.Configure<DProtocolOptions>(option =>
            {
                option = _options;
            });

            builder.Populate(service);

            builder.RegisterType<DProtocol>()
                .As<IExchangeProtocol>()
                .AsSelf();

            return builder.Build();
        }

        private static void ReveivedCtlAction(int cmd, DateTimeOffset dateTime)
        {
        }

        private static void ReceivedPayloadAction(IProtocolPayload payload)
        {
            var shell = JsonConvert.DeserializeObject<PayloadUid>(payload.Text);

            lock (_taskCaches)
            {
                if (_taskCaches.ContainsKey(shell.Uid))
                {
                    _taskCaches[shell.Uid].SetResult(Result.CreateSuccess(payload));
                }
            }
        }

        private static void MockServerSendBuffer(byte[] buffer, int offset, int length)
        {
            // TODO 这里可以模拟下丢包的情况
            _client.PushBuffer(buffer, offset, length);
        }

        private static void MockClientSendBuffer(byte[] buffer, int offset, int length)
        {
            // TODO 这里可以模拟下丢包的情况
            _server.PushBuffer(buffer, offset, length);
        }
    }

    internal interface IPayloadUid
    {
        Guid Uid { get; set; }
    }

    internal class PayloadUid : IPayloadUid
    {
        public Guid Uid { get; set; }
    }

    internal class TxtPayload :
        PayloadUid,
        IPayloadUid
    {
        public string Txt { get; set; }
    }
}
