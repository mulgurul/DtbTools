using System;
using System.IO;
using System.Linq;
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
            var entries = MergeEntry.LoadMergeEntriesFromNcc(Dtb1NccUri);
            foreach (var smil in entries.SelectMany(e => e.DescententsAndSelf).Select(e => e.Smil))
            {
                DtbAudioGenerator.NarrateTextsForSmilFile(smil);
                smil.Save(new Uri(smil.BaseUri).LocalPath);
            }
        }

        private static readonly Uri Dtb1NccUri = new Uri(new Uri(new Uri(Directory.GetCurrentDirectory()), "Out/"), "DTB1/ncc.html");

        [TestMethod]
        public void BuildDtbTest()
        {
            var builder = new DtbBuilder(MergeEntry.LoadMergeEntriesFromNcc(Dtb1NccUri));
            foreach (var smil in builder.SmilFiles.Values)
            {
                DtbAudioGenerator.NarrateTextsForSmilFile(smil);
            }
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
    }
}
