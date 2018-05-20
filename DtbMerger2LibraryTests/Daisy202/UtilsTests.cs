using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using DtbMerger2Library.Daisy202;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DtbMerger2LibraryTests.Daisy202
{
    [TestClass]
    public class UtilsTests
    {
        private static IEnumerable<XElement> GetElements(string localName)
        {
            return new[] {new XElement(localName), new XElement(Utils.XhtmlNs + localName)};
        }

        [TestMethod]
        public void IsHeadingTests()
        {
            foreach (var elem in new[] {"h1", "h2", "h3", "h4", "h5", "h6"}.SelectMany(GetElements))
            {
                Assert.IsTrue(Utils.IsHeading(elem), $"Expected {elem} to be a heading");
            }

            foreach (var elem in new[] {"H1", "p", "span", "hd", "title"}.SelectMany(GetElements))
            {
                Assert.IsFalse(Utils.IsHeading(elem), $"Expected {elem} not to be a heading");
            }
        }

        [TestMethod]
        public void GetSubHeadingNameTests()
        {
            foreach (XNamespace ns in new[] {"", Utils.XhtmlNs})
            {
                for (int i = 1; i < 6; i++)
                {
                    Assert.AreEqual(ns+$"h{i+1}", Utils.GetSubHeadingName(ns + $"h{i}"));
                }
                foreach (var ln in new[] {"H1", "p", "span", "hd", "title"})
                {
                    var sn = Utils.GetSubHeadingName(ns + ln);
                    Assert.IsNull(sn, $"Expected subheading of {ns+ln} to be null, actually was {sn}");
                }
            }
        }

        [TestMethod]
        public void GenerateSkeletonXhtmlDocumentTest()
        {
            var xhtmlDoc = Utils.GenerateSkeletonXhtmlDocument();
            Assert.IsNotNull(xhtmlDoc);
            Assert.IsNotNull(xhtmlDoc.Root);
            Assert.AreEqual(Utils.XhtmlNs + "html", xhtmlDoc.Root.Name);
        }

        [TestMethod]
        public void GenerateSkeletonSmilDocumentTest()
        {
            var smilDoc = Utils.GenerateSkeletonSmilDocument();
            Assert.IsNotNull(smilDoc);
            Assert.IsNotNull(smilDoc.Root);
            Assert.AreEqual("smil", smilDoc.Root.Name);
        }

        [TestMethod]
        public void IsReferenceToTest()
        {
            var testData = new[]
            {
                new Tuple<Uri, Uri, bool>(new Uri("http://temp.org/content.html#id"), new Uri("http://temp.org/ncc.html"), false),
                new Tuple<Uri, Uri, bool>(new Uri("ncc.html#id", UriKind.Relative), new Uri("ncc.html", UriKind.Relative), true),
                new Tuple<Uri, Uri, bool>(new Uri("content.html#id", UriKind.Relative), new Uri("ncc.html", UriKind.Relative), false),
                new Tuple<Uri, Uri, bool>(new Uri("ncc.html#id", UriKind.Relative), new Uri("http://temp.org/ncc.html"), false),
                new Tuple<Uri, Uri, bool>(new Uri("http://temp.org/ncc.html"), new Uri("http://temp.org/ncc.html"), true),
                new Tuple<Uri, Uri, bool>(new Uri("http://temp.org/ncc.html#id"), new Uri("http://temp.org/ncc.html"), true),
                new Tuple<Uri, Uri, bool>(new Uri("http://temp.org/ncc.html"), new Uri("http://temp.org/ncc.html#id"), true),
            };
            foreach (var test in testData)
            {
                Assert.AreEqual(
                    test.Item3, 
                    Utils.IsReferenceTo(test.Item1, test.Item2),
                    $"Expedted IsReferenceTo({test.Item1}, {test.Item2}) to return {test.Item3}");
            }
        }

    }
}
