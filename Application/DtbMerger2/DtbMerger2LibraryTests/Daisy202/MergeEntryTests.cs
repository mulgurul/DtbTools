using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using DtbMerger2Library.Daisy202;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DtbMerger2LibraryTests.Daisy202
{
    [TestClass]
    [DeploymentItem(@".\DTBs")]
    public class MergeEntryTests
    {
        private readonly Uri dtb1NccUri = new Uri(new Uri(new Uri(Directory.GetCurrentDirectory()), "Out/"), "DTB1/ncc.html");
        private readonly Uri macroUri = new Uri(new Uri(new Uri(Directory.GetCurrentDirectory()), "Out/"), "macro.xml");
        private readonly Uri macroContentLinksUri = new Uri(new Uri(new Uri(Directory.GetCurrentDirectory()), "Out/"), "macro_content_links.xml");


        [TestMethod]
        public void NccElementsTest()
        {
            var entry = new MergeEntry() { SourceNavEntry = new UriBuilder(dtb1NccUri) { Fragment = "nav1" }.Uri };
            var nccElements = entry.NccElements?.ToList();
            Assert.IsNotNull(nccElements, "Found ncc elements is null");
            Assert.IsTrue(nccElements.Any(), "Found no ncc elements");
            Assert.AreEqual(1, nccElements.Count(), "Expected 1 ncc element");
            Assert.IsFalse(String.IsNullOrEmpty(nccElements.First().BaseUri), "First ncc element has no base uri");
            Assert.AreEqual(nccElements.First().Attribute("id")?.Value, "nav1");
            Assert.AreEqual(nccElements.First().Attribute("class")?.Value, "title");
            Assert.AreEqual(nccElements.First().Name.LocalName, "h1");
            entry = new MergeEntry() { SourceNavEntry = new UriBuilder(dtb1NccUri) { Fragment = "nav3_2" }.Uri };
            Assert.IsNotNull(nccElements, "Found ncc elements is null");
            Assert.IsTrue(nccElements.Any(), "Found no ncc elements");
            Assert.AreEqual(1, nccElements.Count(), "Expected 1 ncc element");
        }

        [TestMethod]
        public void SmilElementsTest()
        {
            foreach (var frag in new[] {"nav1", "nav3_2"})
            {
                var entry = new MergeEntry() { SourceNavEntry = new UriBuilder(dtb1NccUri) { Fragment = frag }.Uri };
                var smilElements = entry.SmilElements?.ToList();
                Assert.IsNotNull(smilElements, "Smil elements null");
                Assert.IsTrue(smilElements.Any(), "Found no smil elements");
                Assert.IsTrue(smilElements.All(e => e.Name.LocalName == "par"));
            }
        }

        [TestMethod]
        public void AudioSegmentsTest()
        {
            var entry = new MergeEntry() { SourceNavEntry = new UriBuilder(dtb1NccUri) { Fragment = "nav1" }.Uri };
            var audioSegments = entry.AudioSegments.ToList();
            Assert.IsNotNull(audioSegments, "Audio segments elements null");
            Assert.IsTrue(audioSegments.Any(), "Found no audio segments");
            Assert.AreEqual(1, audioSegments.Count(), "Expected one audio segment in DTB1 first heading");
        }

        [TestMethod]
        public void GetTextElementsTest()
        {
            var entry = new MergeEntry() { SourceNavEntry = new UriBuilder(dtb1NccUri) { Fragment = "nav1" }.Uri };
            var textElements = entry.TextElements?.ToList();
            Assert.IsNotNull(textElements, "Text elements is null");
            Assert.IsTrue(textElements.Any(), "Found no text elements");
            Assert.IsTrue(textElements.All(e => e.Parent?.Name == (e.Name.Namespace + "body")));
            Assert.AreEqual(3, textElements.Count(), "Expected 3 text elements, including unreferenced image");
            entry = new MergeEntry() { SourceNavEntry = new UriBuilder(dtb1NccUri) { Fragment = "nav3_2" }.Uri };
            textElements = entry.TextElements?.ToList();
            Assert.IsNotNull(textElements, "Text elements is null");
            Assert.IsTrue(textElements.Any(), "Found no text elements");
            Assert.IsTrue(textElements.All(e => e.Parent?.Name == (e.Name.Namespace + "body")));
            Assert.AreEqual(5, textElements.Count(), "Expected 5 text elements, including table");
            entry = new MergeEntry() { SourceNavEntry = new UriBuilder(dtb1NccUri) { Fragment = "nav4" }.Uri };
            textElements = entry.TextElements?.ToList();
            Assert.IsNotNull(textElements, "Text elements is null");
            Assert.IsTrue(textElements.Any(), "Found no text elements");
            Assert.IsTrue(textElements.All(e => e.Parent?.Name == (e.Name.Namespace + "body")));
            Assert.AreEqual(4, textElements.Count(), "Expected 4 text elements");
            Assert.AreEqual(
                2, 
                textElements.Select(te => te.Document).Distinct().Count(),
                "Expected text elements from two content documents");

        }

        [TestMethod]
        public void NccTest()
        {
            foreach (var frag in new[] {"nav1", "nav3_2"})
            {
                var entry = new MergeEntry() { SourceNavEntry = new UriBuilder(dtb1NccUri) { Fragment = frag }.Uri };
                Assert.IsNotNull(entry.Ncc, "Entry has null Ncc");
                Assert.IsNotNull(entry.Ncc.BaseUri, "Entry Ncc has null BaseUri");
                Assert.IsTrue(Uri.IsWellFormedUriString(Uri.EscapeUriString(entry.Ncc.BaseUri), UriKind.Absolute), $"Entrys Ncc does not have a well formed absolute BaseUri: {entry.Ncc.BaseUri}");
                Assert.AreEqual("html", entry.Ncc.Root?.Name?.LocalName);
            }
        }

        [TestMethod]
        public void SmilTest()
        {
            foreach (var frag in new[] {"nav1", "nav3_2"})
            {
                var entry = new MergeEntry() {SourceNavEntry = new UriBuilder(dtb1NccUri) {Fragment = frag}.Uri};
                Assert.IsNotNull(entry.Smil, "Entry has null Smil");
                Assert.IsNotNull(entry.Smil.BaseUri, "Entry Smil has null BaseUri");
                Assert.IsTrue(Uri.IsWellFormedUriString(Uri.EscapeUriString(entry.Smil.BaseUri), UriKind.Absolute),
                    $"Entrys Ncc does not have a well formed absolute BaseUri: {entry.Smil.BaseUri}");
                Assert.AreEqual("smil", entry.Smil.Root?.Name?.LocalName);
            }
        }

        [TestMethod]
        public void LoadMergeEntriesFromNccTest()
        {
            var entries = MergeEntry.LoadMergeEntriesFromNcc(dtb1NccUri)?.ToList();
            Assert.IsNotNull(entries);
            Assert.AreEqual(4, entries.Count());
            Assert.IsTrue(entries.All(e => e.NccElements.First().Name.LocalName == "h1"), "One loaded entry did not have h1 as first ncc element");
            Assert.AreEqual(0, entries.Last().ChildNodes.Count, "Expected last entry to have no children");
            Assert.AreEqual(8, entries.SelectMany(e => new[] { e }.Union(e.Descendents)).Count(), "Expected a total of 7 entries");
        }

        [TestMethod]
        [Ignore]//Need to map C:\Users\oha\VirtualBlizzardDrive to D: using subst D: C:\Users\oha\VirtualBlizzardDrive
        public void LoadMergeEntriesFromBerlMacroTest()
        {
            var entries = MergeEntry.LoadMergeEntriesFromMacro(new Uri(
                @"D:\BlizzardData\batch\BERL\Publisher\20180516_143247_001\merge.xml")).ToList();
            Assert.IsNotNull(entries, "Loaded entries was null");
            Assert.IsTrue(entries.SelectMany(e => e.NccElements).Any(), "Found no ncc elements");
            Assert.IsTrue(entries.SelectMany(e => e.SmilElements).Any(), "Found no smil elements");
            Assert.IsTrue(entries.SelectMany(e => e.AudioSegments).Any(), "Found no audio segments");
            Assert.IsTrue(entries.SelectMany(e => e.TextElements).Any(), "Found no text elements");
        }

        [TestMethod]
        public void LoadMacroTest()
        {
            LoadMacroTest(macroUri);
        }

        private void LoadMacroTest(Uri uri)
        {
            var macro = XDocument.Load(uri.AbsoluteUri, LoadOptions.SetBaseUri);
            var mergeEntries = MergeEntry.LoadMergeEntriesFromMacro(macro).ToList();
            CheckMergeEntriesLoadedFromMacro(macro, mergeEntries);
            mergeEntries = MergeEntry.LoadMergeEntriesFromMacro(macro).ToList();
            CheckMergeEntriesLoadedFromMacro(macro, mergeEntries);
        }

        private void CheckMergeEntriesLoadedFromMacro(XDocument macro, List<MergeEntry> mergeEntries)
        {
            var expectedMergeEntryCount = macro.Root?.Elements().Count() ?? 0;
            Assert.AreEqual(expectedMergeEntryCount, mergeEntries.Count(), $"Expected {expectedMergeEntryCount} merge entries");
            Assert.IsTrue(
                mergeEntries.Select(me => new Uri(me.Ncc.BaseUri.ToLowerInvariant()).AbsolutePath)
                    .All(baseUri => baseUri.EndsWith("/ncc.htm") || baseUri.EndsWith("/ncc.html")),
                "Some merge entries were loaded from non-ncc documents");
            foreach (var fileAttr in macro.Elements().SelectMany(e => e.Attributes("file")))
            {
                fileAttr.Value = Path.Combine(Directory.GetCurrentDirectory(), fileAttr.Value);
            }
        }

        [TestMethod]
        public void LoadMacroWithContentLinksTest()
        {
            LoadMacroTest(macroContentLinksUri);
        }
    }
}
