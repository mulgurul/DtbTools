using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using DtbSynthesizerLibrary;
using DtbSynthesizerLibrary.Xhtml;
using DtbSynthesizerLibrary.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NAudio.Wave;

namespace DtbSynthesizerLibraryTests.Xhtml
{
    [TestClass]
    [DeploymentItem(@".\TestFiles")]
    [DeploymentItem(@".\libmp3lame.32.dll")]
    [DeploymentItem(@".\libmp3lame.64.dll")]
    public class EpubXhtmlSynthesizerTests
    {
        public TestContext TestContext { get; set; }

        private EpubXhtmlSynthesizer GetEpubSynthesizer(string xhtmlFile)
        {
            xhtmlFile = Path.Combine(TestContext.DeploymentDirectory, xhtmlFile);
            TestContext.AddResultFile(xhtmlFile);
            var audioFileSrc = $"{Path.GetFileNameWithoutExtension(xhtmlFile)}.wav";
            var audioFile = Path.Combine(TestContext.DeploymentDirectory, audioFileSrc);
            TestContext.AddResultFile(audioFile);
            var synthesizer = new EpubXhtmlSynthesizer()
            {
                XhtmlDocument = XDocument.Load(xhtmlFile, LoadOptions.SetBaseUri | LoadOptions.SetLineInfo),
                AudioFileSrc = audioFileSrc,
                AudioWriter = new WaveFileWriter(audioFile, new WaveFormat(22050, 1))
            };
            if (synthesizer.Body.Elements().Select(Utils.SetMissingIds).Sum() > 0)
            {
                synthesizer.XhtmlDocument.Save(
                    new Uri(synthesizer.XhtmlDocument.BaseUri).LocalPath);
            }
            return synthesizer;
        }

        [DataRow(@"Simple\Simple.html")]
        [DataRow(@"Sectioned\Sectioned.html")]
        [DataRow(@"Tables\Tables.html")]
        [DataRow(@"Pages\Pages.html")]
        [DataTestMethod]
        public void MediaOverlayDocumentTest(string xhtmlFile)
        {
            var synthesizer = GetEpubSynthesizer(xhtmlFile);
            synthesizer.Synthesize();
            Assert.AreEqual(1, synthesizer.AudioFiles.Count(), "EpubSynthesizer should produce one audio file per xhtml file");
            Assert.IsNotNull(synthesizer.MediaOverlayDocument, "MediaOverlayDocument is null");
            Assert.AreEqual(
                synthesizer.Body.Descendants().Count(e =>
                    e.AncestorsAndSelf().All(a => a.Annotation<SyncAnnotation>() == null) &&
                    !String.IsNullOrEmpty(e.Attribute("id")?.Value)),
                synthesizer.MediaOverlayDocument.Descendants(EpubXhtmlSynthesizer.Smil30Ns + "seq").Count(),
                "Unecpected number of seq elements in MediaOverlayDocument");
            Assert.AreEqual(
                synthesizer.Body.Descendants().Count(e => e.Annotation<SyncAnnotation>()!=null),
                synthesizer.MediaOverlayDocument.Descendants(EpubXhtmlSynthesizer.Smil30Ns+"par").Count(),
                "Unecpected number of par elements in MediaOverlayDocument");
            Assert.AreEqual(
                synthesizer.Body.Descendants().Count(e => !(e.Ancestors().Any(a => a.Annotation<SyncAnnotation>()!=null) || String.IsNullOrEmpty(e.Attribute("id")?.Value))),
                synthesizer.MediaOverlayDocument?.Root?.Descendants().Count(e => new[]{"seq", "par"}.Select(n => EpubXhtmlSynthesizer.Smil30Ns+n).Contains(e.Name))??0,
                "Unexpected number of seq/par elements in MediaOverlayDocument");
        }
    }
}
