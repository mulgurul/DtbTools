using System;
using System.Collections;
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
            var nccElements = entry.GetNccElements()?.ToList();
            Assert.IsNotNull(nccElements, "Found ncc elements is null");
            Assert.IsTrue(nccElements.Any(), "Found no ncc elements");
            Assert.AreEqual(1, nccElements.Count(), "Expected 1 ncc element");
            Assert.IsFalse(String.IsNullOrEmpty(nccElements.First().BaseUri), "First ncc element has no base uri");
            Assert.AreEqual(nccElements.First().Attribute("id")?.Value, "nav1");
            Assert.AreEqual(nccElements.First().Attribute("class")?.Value, "title");
            Assert.AreEqual(nccElements.First().Name.LocalName, "h1");
        }

        [TestMethod]
        public void GetMediaEntriesTest()
        {
            var entry = new MergeEntry() {SourceNavEntry = new UriBuilder(dtb1NccUri) { Fragment = "nav1" }.Uri};
            var mediaEntries = entry.GetMediaEntries()?.ToList();
            Assert.IsNotNull(mediaEntries, "Media entries is null");
            Assert.IsTrue(mediaEntries.Any(), "Found no media entries");
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
        public void GetTextElementsTest()
        {
            var entry = new MergeEntry() { SourceNavEntry = new UriBuilder(dtb1NccUri) { Fragment = "nav1" }.Uri };
            var textElements = entry.GetTextElements()?.ToList();
            Assert.IsNotNull(textElements, "Text elements is null");
            Assert.IsTrue(textElements.Any(), "Found no text elements");
            Assert.IsTrue(textElements.All(e => e.Parent?.Name == (e.Name.Namespace+"body")));
            Assert.AreEqual(3, textElements.Count(), "Expected 3 text elements, including unreferenced image");
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
            Assert.IsTrue(Uri.IsWellFormedUriString(Uri.EscapeUriString(entry.Smil.BaseUri), UriKind.Absolute), $"Entrys Ncc does not have a well formed absolute BaseUri: {entry.Smil.BaseUri}");
            Assert.AreEqual("smil", entry.Smil.Root?.Name?.LocalName);
        }

        [TestMethod]
        public void LoadMergeEntriesFromNccTest()
        {
            var entries = MergeEntry.LoadMergeEntriesFromNcc(dtb1NccUri)?.ToList();
            Assert.IsNotNull(entries);
            Assert.AreEqual(3, entries.Count());
            Assert.IsTrue(entries.All(e => e.GetNccElements().First().Name.LocalName == "h1"), "One loaded entry did not have h1 as first ncc element");
            Assert.AreEqual(2, entries.Last().ChildNodes.Count, "Expected last entry to have two children");
            Assert.AreEqual(7, entries.SelectMany(e => new[] { e }.Union(e.Descendents)).Count(), "Expected a total of 7 entries");
        }

        [TestMethod]
        //[Ignore]//Need to map C:\Users\oha\VirtualBlizzardDrive to D: using subst D: C:\Users\oha\VirtualBlizzardDrive
        public void LoadMergeEntriesFromBERLMacroTest()
        {
            var entries = MergeEntry.LoadMergeEntriesFromMacro(new Uri(
                @"D:\BlizzardData\batch\BERL\Publisher\20180516_143247_001\merge.xml")).ToList();
            Assert.IsNotNull(entries, "Loaded entries was null");
            Assert.IsTrue(entries.SelectMany(e => e.GetNccElements()).Any(), "Found no ncc elements");
            Assert.IsTrue(entries.SelectMany(e => e.GetSmilElements()).Any(), "Found no smil elements");
            Assert.IsTrue(entries.SelectMany(e => e.GetAudioSegments()).Any(), "Found no audio segments");
            Assert.IsTrue(entries.SelectMany(e => e.GetTextElements()).Any(), "Found no text elements");
        }
    }
}
