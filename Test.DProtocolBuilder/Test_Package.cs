using D.FreeExchange.Protocol.DP;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text;

namespace Test.DProtocolBuilder
{
    [TestClass]
    public class Test_Package
    {
        [TestMethod]
        public void Test_Head()
        {
            var p1 = new PackageHeader(PackageCode.Heart, FlagCode.Single);

            var buffer = p1.ToBuffer();

            Assert.AreEqual(buffer.Length, 1);

            var p2 = new PackageHeader(buffer[0]);

            Assert.AreEqual(p1.Flag, p2.Flag);
            Assert.AreEqual(p1.Code, p2.Code);
        }

        [TestMethod]
        public void Test_Head_BigIndex()
        {
            var p1 = new PackageWithIndex(PackageCode.Clean);
            p1.Index = 256;

            var buffer = p1.ToBuffer();

            Assert.AreEqual(buffer.Length, 3);

            var header = new PackageHeader(buffer[0]);
            var p2 = new PackageWithIndex(header);

            var index = 1;
            var need = p2.PushBuffer(buffer, ref index, buffer.Length);

            Assert.AreEqual(need, 0);
            Assert.AreEqual(p1.Flag, p2.Flag);
            Assert.AreEqual(p1.Code, p2.Code);
            Assert.AreEqual(p1.Index, p2.Index);
        }

        [TestMethod]
        public void Test_ConnectPak()
        {
            Encoding encoding = Encoding.Default;

            var pak1Data = new ConnectPackageData
            {
                Uid = "pak1",
                Options = new D.FreeExchange.DProtocolOptions()
            };

            var pak1 = new ConnectPackage();
            pak1.SetData(pak1Data, encoding);

            var buffer = pak1.ToBuffer();

            var header = new PackageHeader(buffer[0]);
            var pak2 = new ConnectPackage(header);

            var index = 1;
            pak2.PushBuffer(buffer, ref index, buffer.Length - 1);

            var pak2Data = pak2.GetData(encoding);

            Assert.AreEqual(pak1Data.Uid, pak2Data.Uid);
        }
    }
}
