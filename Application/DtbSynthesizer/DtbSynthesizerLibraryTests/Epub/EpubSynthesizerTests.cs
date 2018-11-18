using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using DtbSynthesizerLibrary;
using DtbSynthesizerLibrary.Epub;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DtbSynthesizerLibraryTests.Epub
{
    [TestClass]
    [DeploymentItem(@".\TestFiles")]
    [DeploymentItem(@".\libmp3lame.32.dll")]
    [DeploymentItem(@".\libmp3lame.64.dll")]
    public class EpubSynthesizerTests
    {
        public TestContext TestContext { get; set; }

        public string ResolvePath(string path)
        {
            if (path.StartsWith(@"~\"))
            {
                return Path.Combine(Environment.GetEnvironmentVariable("USERPROFILE") ?? @".\", path.Substring(2));
            }
            return Path.Combine(TestContext.DeploymentDirectory, path);

        }

        [DataRow(@"Epub\test.epub")]
        [DataTestMethod]
        public void SynthesizeTests(string epubFile)
        {
            epubFile = ResolvePath(epubFile);
            var output = Path.Combine(TestContext.DeploymentDirectory, "EpubOutput", Path.GetFileName(epubFile)??"");
            if (!Directory.Exists(Path.GetDirectoryName(output)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(output)??"");
            }
            File.Copy(epubFile, output, true);
            TestContext.AddResultFile(output);
            using (var epubPub = new EpubPublication() {Path = output })
            {
                var synth = new EpubSynthesizer() { Publication = epubPub };
                var headerNames = Enumerable.Range(1, 6).Select(i => Utils.XhtmlNs + $"h{i}").ToList();
                synth.Synthesize();
                Assert.AreEqual(
                    synth.Publication.XhtmlDocuments.Count(),
                    synth.Publication.FileUris.Count(uri => uri.AbsolutePath.EndsWith(".smil")),
                    "Expected one smil file per xhtml file");
                Assert.AreEqual(
                    synth.Publication.XhtmlDocuments.SelectMany(doc => doc.Descendants()).Count(e => headerNames.Contains(e.Name)),
                    synth.Publication.FileUris.Count(uri => uri.AbsolutePath.EndsWith(".mp3")),
                    "Expected one audio file per xhtml file");
                Assert.AreEqual(
                    synth.Publication.XhtmlDocuments.SelectMany(doc => doc.Descendants()).Count(e => headerNames.Contains(e.Name)),
                    synth.Publication.PackageFile.Descendants(Utils.OpfNs + "item").Count(item => item.Attribute("href")?.Value?.EndsWith(".mp3") ?? false),
                    "Expected one mp3 item in manifest per xhtml file");
                Assert.AreEqual(
                    synth.Publication.XhtmlDocuments.Count(),
                    synth.Publication.PackageFile.Descendants(Utils.OpfNs + "item").Count(item => item.Attribute("href")?.Value?.EndsWith(".smil") ?? false),
                    "Expected one smil item in manifest per xhtml file");
            }
        }
    }
}
