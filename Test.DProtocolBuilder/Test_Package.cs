using D.FreeExchange;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test.DProtocolBuilder
{
    [TestClass]
    public class Test_Package
    {
        [TestMethod]
        public void Test_Haad()
        {
            var p1 = new Package();
            p1.Fin = true;
            p1.Code = PackageCode.Heart;
            p1.Index = 15;

            var buffer = p1.ToBuffer();

            var p2 = new Package();
            var index = 0;
            var need = p2.PushBuffer(buffer, ref index, buffer.Length);

            Assert.AreEqual(need, 0);
            Assert.AreEqual(p1.Fin, p2.Fin);
            Assert.AreEqual(p1.Code, p2.Code);
            Assert.AreEqual(p1.Index, p2.Index);
        }

        [TestMethod]
        public void Test_Haad_BigIndex()
        {
            var p1 = new Package();
            p1.Fin = true;
            p1.Code = PackageCode.Disconnect;
            p1.Index = 257;

            var buffer = p1.ToBuffer();

            var p2 = new Package();
            var index = 0;
            var need = p2.PushBuffer(buffer, ref index, buffer.Length);

            Assert.AreEqual(need, 0);
            Assert.AreEqual(p1.Fin, p2.Fin);
            Assert.AreEqual(p1.Code, p2.Code);
            Assert.AreEqual(p1.Index, p2.Index);
        }
    }
}
