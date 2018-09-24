using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using DtbSynthesizerLibrary;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DtbSynthesizerLibraryTests
{
    [TestClass]
    [DeploymentItem("grpc_csharp_ext.x86.dll")]
    [DeploymentItem("grpc_csharp_ext.x64.dll")]
    [DeploymentItem(@".\TestFiles")]
    public class UtilsTests
    {
        public TestContext TestContext { get; set; }

        [DataRow("dtbook110to2005-1.xsl")]
        [DataRow("dtbook2005-1to2.xsl")]
        [DataRow("dtbook2005-2to3.xsl")]
        [DataRow("dtbook2xhtml.xsl")]
        [DataTestMethod]
        public void EmbeddedXsltExecutablesTest(string name)
        {
            Assert.IsTrue(Utils.EmbeddedXsltExecutables.ContainsKey(name));
            var xslt = Utils.EmbeddedXsltExecutables[name];
            Assert.IsNotNull(xslt);
        }

        [DataRow(@"dtbook\dtbook110.xml")]
        [DataRow(@"dtbook\dtbook2005-1.xml")]
        [DataRow(@"dtbook\dtbook2005-2.xml")]
        [DataRow(@"dtbook\dtbook2005-3.xml")]
        [DataTestMethod]
        public void TransformDtbookToXhtmlTest(string file)
        {
            file = Path.Combine(TestContext.DeploymentDirectory, file);
            var dtbookDoc = XDocument.Load(file);
            var xhtmlDoc = Utils.TransformDtbookToXhtml(dtbookDoc);
            Assert.IsNotNull(xhtmlDoc, "Transform result is null");
            Assert.IsNotNull(xhtmlDoc.Root, "Transform result root is null");
            Assert.AreEqual(Utils.XhtmlNs+"html", xhtmlDoc.Root.Name, "Transform result is not an xhtml document");
        }

        [TestMethod]
        public void GetHHMMSSFromTimeSpanTest()
        {
            var data = new[]
            {
                new Tuple<TimeSpan, string>(TimeSpan.FromMilliseconds(499), "00:00:00"),
                new Tuple<TimeSpan, string>(TimeSpan.FromMilliseconds(500), "00:00:01"),
                new Tuple<TimeSpan, string>(new TimeSpan(0, 0, 1), "00:00:01"),
                new Tuple<TimeSpan, string>(new TimeSpan(0, 1, 1), "00:01:01"),
                new Tuple<TimeSpan, string>(new TimeSpan(1, 1, 1), "01:01:01"),
                new Tuple<TimeSpan, string>(new TimeSpan(1, 1, 1, 1), "25:01:01")
            };
            foreach (var d in data)
            {
                Assert.AreEqual(d.Item2, Utils.GetHHMMSSFromTimeSpan(d.Item1));
            }
        }

        [DataRow("da", "da", true)]
        [DataRow("da", "en", false)]
        [DataRow("da", "da-DK", false)]
        [DataRow("da-DK", "da", false)]
        [DataTestMethod]
        public void CultureInfoEquals(string ci1, string ci2, bool expectedResult)
        {
            Assert.AreEqual(expectedResult, new CultureInfo(ci1).Equals(new CultureInfo(ci2)));
        }

        [TestMethod]
        public void AllXmlSynhtesizersTest()
        {
            Console.WriteLine(
                Utils.GetAllSynthesizers().Select(s => s.VoiceInfo.Description).Aggregate((s, v) => $"{s}\n{v}"));
        }

    }
}
