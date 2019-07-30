using D.FreeExchange.Protocol.DP;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test.DProtocolBuilder
{
    [TestClass]
    public class Test_Package
    {
        [TestMethod]
        public void Test_Haad()
        {
            var p1 = new PackageHeader(PackageCode.Heart, FlagCode.Single);

            var buffer = p1.ToBuffer();

            Assert.AreEqual(buffer.Length, 1);

            var p2 = new PackageHeader(buffer[0]);

            Assert.AreEqual(p1.Flag, p2.Flag);
            Assert.AreEqual(p1.Code, p2.Code);
        }

        [TestMethod]
        public void Test_Haad_BigIndex()
        {
            var p1 = new Package();
            p1.Flag = FlagCode.End;
            p1.Code = PackageCode.Clean;
            p1.Index = 256;

            var buffer = p1.ToBuffer();

            Assert.AreEqual(buffer.Length, 3);

            var p2 = new Package();
            var index = 0;
            var need = p2.PushBuffer(buffer, ref index, buffer.Length);

            Assert.AreEqual(need, 0);
            Assert.AreEqual(p1.Flag, p2.Flag);
            Assert.AreEqual(p1.Code, p2.Code);
            Assert.AreEqual(p1.Index, p2.Index);
        }
    }
}
