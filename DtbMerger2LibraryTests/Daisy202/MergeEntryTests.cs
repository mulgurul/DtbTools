using System;
using System.IO;
using System.Linq;
using DtbMerger2Library.Daisy202;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DtbMerger2LibraryTests.Daisy202
{
    [TestClass]
    [DeploymentItem(@".\DTBs")]
    public class MergeEntryTests
    {
        private readonly Uri dtb1NccUri = new Uri(new Uri(new Uri(Directory.GetCurrentDirectory()), "Out/"), "DTB1/ncc.html");

        [TestMethod]
        public void GetNccElementsTest()
        {
            var entry = new MergeEntry() { SourceNavEntry = new UriBuilder(dtb1NccUri) { Fragment = "nav1" }.Uri };
            var nccElements = entry.GetNccElements().ToList();
            Assert.IsTrue(nccElements.Any(), "Found no ncc elements");
            Assert.IsFalse(String.IsNullOrEmpty(nccElements.First().BaseUri), "First ncc element has no base uri");
            Assert.AreEqual(nccElements.First().Attribute("id")?.Value, "nav1");
            Assert.AreEqual(nccElements.First().Attribute("class")?.Value, "title");
            Assert.AreEqual(nccElements.First().Name.LocalName, "h1");
        }

        [TestMethod]
        public void GetMediaEntriesTest()
        {
            var entry = new MergeEntry() {SourceNavEntry = new UriBuilder(dtb1NccUri) { Fragment = "nav1" }.Uri};
            var nccElements = entry.GetNccElements().ToList();
            Assert.IsTrue(nccElements.Any(), "Found no ncc elements");
            Assert.IsFalse(String.IsNullOrEmpty(nccElements.First().BaseUri), "First ncc element has no base uri");

        }
    }
}
