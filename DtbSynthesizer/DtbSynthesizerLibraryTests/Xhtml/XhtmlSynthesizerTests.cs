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

        [DataRow(@"Simple\Simple.html")]
        [DataRow(@"Sectioned\Sectioned.html")]
        [DataRow(@"Tables\Tables.html")]
        [DataTestMethod]
        public void SynthesizeTests(string xhtmlFile)
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
            synthesizer.Synthesize();
            Assert.IsTrue(
                synthesizer.Body.Descendants().All(elem => 
                    elem.Annotation<SyncAnnotation>() != null 
                    || synthesizer.BlockContainerNames.Contains(elem.Name))
                );
            Assert.AreEqual(
                synthesizer
                    .Body
                    .Descendants()
                    .Count(elem => synthesizer.HeaderNames.Contains(elem.Name)),
                synthesizer
                    .Body
                    .Descendants()
                    .SelectMany(e => e.Annotations<SyncAnnotation>())
                    .Select(a => a.Src)
                    .Distinct()
                    .Count(),
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
    }
}
