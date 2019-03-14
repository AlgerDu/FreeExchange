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

namespace Test.DProtocolBuilder
{
    [TestClass]
    public class Test_DP
    {
        readonly IContainer _container;

        IProtocolBuilder _ptlBuilder;

        Dictionary<Guid, TaskCompletionSource<IProtocolPayload>> _taskCaches;

        public Test_DP()
        {
            _container = CreateContainer();

            _ptlBuilder = _container.Resolve<IProtocolBuilder>();

            _ptlBuilder.SetReceivedControlAction(ReveivedCtlAction);
            _ptlBuilder.SetReceivedPayloadAction(ReceivedPayloadAction);
            _ptlBuilder.SetSendBufferAction(MockSendBuffer);

            _taskCaches = new Dictionary<Guid, TaskCompletionSource<IProtocolPayload>>();
        }

        [TestMethod]
        public void TestSimpleDP()
        {
            var model = new PayloadUid
            {
                Uid = Guid.NewGuid()
            };

            var payload = SenAndReceive(model);

            Assert.AreEqual(payload.Text, JsonConvert.SerializeObject(model));
            Assert.AreEqual(payload.Bytes.Count(), 0);
        }

        private IProtocolPayload SenAndReceive<T>(
            T dataWithUid
            , params IByteDescription[] bytes
            )
            where T : IPayloadUid
        {
            var tcs = new TaskCompletionSource<IProtocolPayload>();

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

                    _ptlBuilder.PushPayload(payload);
                });
            }

            tcs.Task.Wait();

            return tcs.Task.Result;
        }

        private IContainer CreateContainer()
        {
            var builder = new ContainerBuilder();

            builder.AddMicrosoftExtensions();

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
                    _taskCaches[shell.Uid].SetResult(payload);
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
}
