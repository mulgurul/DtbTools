using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using DtbMerger2Library.Daisy202;
using DtbMerger2LibraryTests.DTBs;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DtbMerger2LibraryTests.Daisy202
{
    [TestClass]
    [DeploymentItem(@".\DTBs")]
    [DeploymentItem(@".\libmp3lame.32.dll")]
    [DeploymentItem(@".\libmp3lame.64.dll")]
    public class BuildDtbTests
    {
        [ClassInitialize]
        public static void InitializeClass(TestContext context)
        {
            var totelElapsedTime = TimeSpan.Zero;
            var entries = MergeEntry.LoadMergeEntriesFromNcc(Dtb1NccUri).ToList();
            foreach (var smil in entries.SelectMany(e => e.DescententsAndSelf).Select(e => e.Smil))
            {
                DtbAudioGenerator.NarrateTextsForSmilFile(smil, ref totelElapsedTime);
                smil.Save(new Uri(smil.BaseUri).LocalPath);
            }
            var nccHead = entries.First().Ncc.Root?.Element(Utils.XhtmlNs + "head");
            if (nccHead != null)
            {
                var ttMeta = nccHead
                    .Elements(Utils.XhtmlNs + "meta")
                    .FirstOrDefault(m => m.Attribute("name")?.Value == "nccHead:totalTime");
                if (ttMeta == null)
                {
                    ttMeta = new XElement(Utils.XhtmlNs+"meta", new XAttribute("name", "nccHead:totalTime"));
                    nccHead.Add(ttMeta);
                }

            }
        }

        private static readonly Uri Dtb1NccUri = new Uri(new Uri(new Uri(Directory.GetCurrentDirectory()), "Out/"), "DTB1/ncc.html");

        [TestMethod]
        public void BuildDtbTest()
        {
            var builder = new DtbBuilder(MergeEntry.LoadMergeEntriesFromNcc(Dtb1NccUri));
            builder.BuildDtb();
            ValidateBuiltDtb(builder);

        }

        [TestMethod]
        public void SaveDtbTest()
        {
            var builder = new DtbBuilder(MergeEntry.LoadMergeEntriesFromNcc(Dtb1NccUri));
            builder.BuildDtb();
            builder.SaveDtb("./MergedDTB");
            ValidateSavedDtb(builder, "./MergedDTB");
        }

        [TestMethod]
        [Ignore]//Need to map C:\Users\oha\VirtualBlizzardDrive to D: using subst D: C:\Users\oha\VirtualBlizzardDrive
        public void SaveBerlDtbTest()
        {
            var builder = new DtbBuilder(MergeEntry.LoadMergeEntriesFromMacro(new Uri(
                @"D:\BlizzardData\batch\BERL\Publisher\20180516_143247_001\merge.xml")));
            builder.BuildDtb();
            builder.SaveDtb("./MergedBERLDTB");
            ValidateSavedDtb(builder, "./MergedBERLDTB");
        }

        private void ValidateBuiltDtb(DtbBuilder builder)
        {
            var entries = builder.MergeEntries.SelectMany(me => me.DescententsAndSelf).ToList();
            Assert.AreEqual(entries.Count, builder.SmilFiles.Count, "Expected one smil file per merge file entry");
            Assert.AreEqual(entries.Count, builder.AudioFileSegments.Count, "Expected one audio file per merge file entry");
            var nccHeadings = builder.NccDocument.Root?.Element(Utils.XhtmlNs + "body")?.Elements()
                .Where(Utils.IsHeading).ToList()??new List<XElement>();
            Assert.AreEqual(entries.Count, nccHeadings?.Count??0, "Expected one heading in built ncc per merge entry");
            for (int i = 0; i < entries.Count; i++)
            {
                Assert.AreEqual(
                    Utils.XhtmlNs+$"h{Math.Min(entries[i].Depth, 6)}", 
                    nccHeadings[i].Name, 
                    $"Expected heading {nccHeadings[i]} at index {i} to be a {Utils.XhtmlNs+$"h{ entries[i].Depth}"} heading");
            }

            foreach (var audio in builder.SmilFiles.Values
                .SelectMany(doc => doc.Descendants("audio"))
                .Where(audio => audio.Attribute("src") != null))
            {
                Assert.IsTrue(builder.AudioFileSegments.Keys.Contains(audio.Attribute("src")?.Value), $"Found no audio segment matching {audio} in {audio.BaseUri}");
            }
        }

        private void ValidateSavedDtb(DtbBuilder builder, string destDir)
        {
            ValidateBuiltDtb(builder);
            foreach (var xmlFileName in builder.XmlDocuments.Keys)
            {
                Assert.IsTrue(File.Exists(Path.Combine(destDir, xmlFileName)));
            }

            if (builder.ContentDocument != null)
            {
                foreach (var imgSrcAttr in builder.ContentDocument.Descendants(Utils.XhtmlNs + "img")
                    .Select(img => img.Attribute("src"))
                    .Where(a => a != null))
                {
                    Assert.IsTrue(File.Exists(Path.Combine(destDir, imgSrcAttr.Value)), $"Image for {imgSrcAttr.Parent} not found");
                }
            }

        }
    }
}
