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

        [TestMethod]
        public void GetSmilElementsTest()
        {
            var entry = new MergeEntry() { SourceNavEntry = new UriBuilder(dtb1NccUri) { Fragment = "nav1" }.Uri };
            var smilElements = entry.GetSmilElements()?.ToList();
            Assert.IsNotNull(smilElements, "Smil elements null");
            Assert.IsTrue(smilElements.Any(), "Found no smil elements");
            Assert.IsTrue(smilElements.All(e => e.Name.LocalName == "par"));
        }

        [TestMethod]
        public void GetAudioSegmentsTest()
        {
            var entry = new MergeEntry() { SourceNavEntry = new UriBuilder(dtb1NccUri) { Fragment = "nav1" }.Uri };
            var audioSegments = entry.GetAudioSegments().ToList();
            Assert.IsNotNull(audioSegments, "Audio segments elements null");
            Assert.IsTrue(audioSegments.Any(), "Found no audio segments");
            Assert.AreEqual(1, audioSegments.Count(), "Expected one audio segment in DTB1 first heading");
        }

        [TestMethod]
        public void NccTest()
        {
            var entry = new MergeEntry() { SourceNavEntry = new UriBuilder(dtb1NccUri) { Fragment = "nav1" }.Uri };
            Assert.IsNotNull(entry.Ncc, "Entry has null Ncc");
            Assert.IsNotNull(entry.Ncc.BaseUri, "Entry Ncc has null BaseUri");
            Assert.IsTrue(Uri.IsWellFormedUriString(Uri.EscapeUriString(entry.Ncc.BaseUri), UriKind.Absolute), $"Entrys Ncc does not have a well formed absolute BaseUri: {entry.Ncc.BaseUri}");
            Assert.AreEqual("html", entry.Ncc.Root?.Name?.LocalName);
        }

        [TestMethod]
        public void SmilTest()
        {
            var entry = new MergeEntry() { SourceNavEntry = new UriBuilder(dtb1NccUri) { Fragment = "nav1" }.Uri };
            Assert.IsNotNull(entry.Smil, "Entry has null Smil");
            Assert.IsNotNull(entry.Smil.BaseUri, "Entry Smil has null BaseUri");
            Assert.IsTrue(Uri.IsWellFormedUriString(Uri.EscapeUriString(entry.Smil.BaseUri), UriKind.Absolute), $"Entrys Ncc does not have a well formed absolute BaseUri: {entry.Ncc.BaseUri}");
            Assert.AreEqual("smil", entry.Smil.Root?.Name?.LocalName);
        }
    }
}
