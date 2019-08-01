using Autofac;
using D.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using D.FreeExchange;
using D.Utils;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Linq;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using Autofac.Extensions.DependencyInjection;
using NLog.Extensions.Logging;
using Microsoft.Extensions.Logging;

namespace Test.DProtocolBuilder
{
    [TestClass]
    public class Test_DP
    {
        readonly IContainer _container;

        IExchangeProtocol _client;
        IExchangeProtocol _server;

        DProtocolOptions _options;

        Dictionary<Guid, TaskCompletionSource<IResult<IProtocolPayload>>> _taskCaches;

        public Test_DP()
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

            _client.Run(ExchangeProtocolRunningMode.Client);
            _server.Run(ExchangeProtocolRunningMode.Server);

            _taskCaches = new Dictionary<Guid, TaskCompletionSource<IResult<IProtocolPayload>>>();
        }

        [TestMethod]
        public void TestSimpleDP()
        {
            var model = new PayloadUid
            {
                Uid = Guid.NewGuid()
            };

            var rst = SenAndReceive(model);
            var payload = rst.Data;

            Assert.AreEqual(payload.Text, JsonConvert.SerializeObject(model));
            Assert.AreEqual(payload.Bytes.Count(), 0);
        }

        /// <summary>
        /// 发送的数据超过了发送缓存区
        /// </summary>
        [TestMethod]
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

            Assert.AreEqual(rst.IsSuccess(), false);
            Assert.AreEqual(rst.Code, (int)ExchangeCode.SentBufferFull);
        }

        private IResult<IProtocolPayload> SenAndReceive<T>(
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

        private IContainer CreateContainer()
        {
            var builder = new ContainerBuilder();
            var service = new ServiceCollection();

            service.AddOptions();

            service.AddLogging(logging =>
            {
                logging.AddNLog();
                logging.SetMinimumLevel(LogLevel.Trace);
            });

            _options = new DProtocolOptions();

            service.Configure<DProtocolOptions>(option =>
            {
                option = _options;
            });

            builder.Populate(service);

            builder.AddMicrosoftExtensions();

            builder.RegisterType<DProtocol>()
                .As<IExchangeProtocol>()
                .AsSelf();

            return builder.Build();
        }

        private void ReveivedCtlAction(int cmd, DateTimeOffset dateTime)
        {
        }

        private void ReceivedPayloadAction(IProtocolPayload payload)
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

        private void MockServerSendBuffer(byte[] buffer, int offset, int length)
        {
            // TODO 这里可以模拟下丢包的情况
            _client.PushBuffer(buffer, offset, length);
        }

        private void MockClientSendBuffer(byte[] buffer, int offset, int length)
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
