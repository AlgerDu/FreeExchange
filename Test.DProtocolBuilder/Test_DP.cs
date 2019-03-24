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

namespace Test.DProtocolBuilder
{
    [TestClass]
    public class Test_DP
    {
        readonly IContainer _container;

        IProtocolBuilder _ptlBuilder;
        DProtocolBuilderOptions _options;

        Dictionary<Guid, TaskCompletionSource<IResult<IProtocolPayload>>> _taskCaches;

        public Test_DP()
        {
            _options = new DProtocolBuilderOptions
            {
                HeartInterval = 5,
                MaxPackageBuffer = 4,
                MaxPayloadDataLength = 32
            };

            _container = CreateContainer();

            _ptlBuilder = _container.Resolve<IProtocolBuilder>();

            _ptlBuilder.SetReceivedControlAction(ReveivedCtlAction);
            _ptlBuilder.SetReceivedPayloadAction(ReceivedPayloadAction);
            _ptlBuilder.SetSendBufferAction(MockSendBuffer);

            _ptlBuilder.Run();

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

                    var task = _ptlBuilder.PushPayload(payload);
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

            builder.AddMicrosoftExtensions();

            builder.RegisterType<D.FreeExchange.DProtocolBuilder>()
                .As<IProtocolBuilder>()
                .AsSelf();

            builder.RegisterInstance<IOptions<DProtocolBuilderOptions>>(
                Options.Create<DProtocolBuilderOptions>(_options)
                );


            return builder.Build();
        }

        private void ReveivedCtlAction(int cmd)
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

        private void MockSendBuffer(byte[] buffer, int offset, int length)
        {
            _ptlBuilder.PushBuffer(buffer, offset, length);
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
