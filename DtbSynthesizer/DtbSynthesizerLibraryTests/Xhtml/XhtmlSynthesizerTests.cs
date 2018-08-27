using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using DtbSynthesizerLibrary;
using DtbSynthesizerLibrary.Xhtml;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DtbSynthesizerLibraryTests.Xhtml
{
    [TestClass]
    [DeploymentItem(@".\TestFiles")]
    public class XhtmlSynthesizerTests
    {
        public TestContext TestContext { get; set; }

        private XhtmlSynthesizer GetXhtmlSynthesizer(string xhtmlFile)
        {
            xhtmlFile = Path.Combine(TestContext.DeploymentDirectory, xhtmlFile);
            TestContext.AddResultFile(xhtmlFile);
            var synthesizer = new XhtmlSynthesizer()
            {
                XhtmlDocument = XDocument.Load(xhtmlFile, LoadOptions.SetBaseUri | LoadOptions.SetLineInfo)
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
        [DataTestMethod]
        public void SynthesizeTest(string xhtmlFile)
        {
            var synthesizer = GetXhtmlSynthesizer(xhtmlFile);
            synthesizer.Synthesize();
            Assert.IsTrue(
                synthesizer.Body.DescendantNodes().OfType<XText>()
                    .All(text => text.Annotation<SyncAnnotation>() != null));
            Assert.AreEqual(
                synthesizer
                    .Body
                    .Descendants()
                    .Count(elem => synthesizer.HeaderNames.Contains(elem.Name)),
                synthesizer.AudioFiles.Count(),
                "Expected one audio file per heading");
            Console.WriteLine(
                $"Xhtml file {Path.GetFileName(new Uri(synthesizer.XhtmlDocument.BaseUri).LocalPath)}");

            foreach (var elem in synthesizer.Body.Descendants()
                .Where(e => e.Attribute("id")?.Value != null))
            {
                var anno = elem.Annotation<SyncAnnotation>();
                if (anno != null)
                {
                    Console.WriteLine(
                        $"#{elem.Attribute("id")?.Value}={anno.Src}:{anno.ClipBegin}-{anno.ClipEnd}");
                }
            }
            foreach (var audioFile in synthesizer
                .Body
                .Descendants().SelectMany(e => e.Annotations<SyncAnnotation>().Select(a =>
                    new Uri(new Uri(e.BaseUri), a.Src).LocalPath)).Distinct())
            {
                TestContext.AddResultFile(audioFile);
            }
        }

        [DataRow(@"Simple\Simple.html")]
        [DataRow(@"Sectioned\Sectioned.html")]
        [DataRow(@"Tables\Tables.html")]
        [DataTestMethod]
        public void GenerateDaisy202SmilFilesAndNccDocumentTest(string xhtmlFile)
        {
            var synthesizer = GetXhtmlSynthesizer(xhtmlFile);
            synthesizer.Synthesize();
            synthesizer.GenerateDaisy202SmilFiles();
            Assert.AreEqual(synthesizer.AudioFiles.Count(), synthesizer.SmilFiles.Count, $"Expected one smil file per audio file");
            synthesizer.GenerateNccDocument();
            Assert.IsNotNull(synthesizer.NccDocument, "No ncc document was generated");
            Assert.AreEqual(
                synthesizer.SmilFiles.Count(),
                synthesizer
                    .NccDocument
                    .Root
                    ?.Element(XhtmlSynthesizer.XhtmlNs+"body")
                    ?.Elements()
                    .Count(e => 
                        Enumerable.Range(1, 6).Select(i => XhtmlSynthesizer.XhtmlNs+ $"h{i}").Contains(e.Name)),
                "Expected one ncc heading per smil file");
            Assert.AreEqual(
                "Daisy 2.02",
                Utils.GetMetaContent(synthesizer.NccDocument, "dc:format"),
                "Expected dc:format=Daisy 2.02 meta");
            Assert.AreEqual(
                (2+synthesizer.AudioFiles.Count()+synthesizer.SmilFiles.Count).ToString(),
                Utils.GetMetaContent(synthesizer.NccDocument, "ncc:files"),
                "Expected dc:format=Daisy 2.02 meta");
        }
    }
}
