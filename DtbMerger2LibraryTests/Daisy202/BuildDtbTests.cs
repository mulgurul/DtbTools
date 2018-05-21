using System;
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
            Assert.AreEqual(builder.MergeEntries.SelectMany(e => e.DescententsAndSelf).Count(), builder.SmilFiles.Count);
            Assert.AreEqual(builder.MergeEntries.SelectMany(e => e.DescententsAndSelf).Count(), builder.AudioFileSegments.Count);

        }

        [TestMethod]
        public void SaveDtbTest()
        {
            var builder = new DtbBuilder(MergeEntry.LoadMergeEntriesFromNcc(Dtb1NccUri));
            builder.BuildDtb();
            builder.SaveDtb("./MergedDTB");
            foreach (var xmlFileName in builder.XmlDocuments.Keys)
            {
                Assert.IsTrue(File.Exists(Path.Combine("./MergedDTB", xmlFileName)));
            }
        }

        [TestMethod]
        [Ignore]//Need to map C:\Users\oha\VirtualBlizzardDrive to D: using subst D: C:\Users\oha\VirtualBlizzardDrive
        public void SaveBERLDtbTest()
        {
            var builder = new DtbBuilder(MergeEntry.LoadMergeEntriesFromMacro(new Uri(
                @"D:\BlizzardData\batch\BERL\Publisher\20180516_143247_001\merge.xml")));
            builder.BuildDtb();
            builder.SaveDtb("./MergedBERLDTB");
            foreach (var xmlFileName in builder.XmlDocuments.Keys)
            {
                Assert.IsTrue(File.Exists(Path.Combine("./MergedBERLDTB", xmlFileName)));
            }
        }
    }
}
