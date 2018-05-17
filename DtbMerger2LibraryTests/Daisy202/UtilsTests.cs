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

    }
}
