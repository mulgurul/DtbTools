using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using DtbSynthesizerLibrary.Xhtml;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DtbSynthesizerLibraryTests.Xhtml
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
        public void PackageFileTests(string epubFile)
        {
            epubFile = ResolvePath(epubFile);
            using (var epubContainer = ZipFile.Open(epubFile, ZipArchiveMode.Update))
            {
                var synth = new EpubSynthesizer() { EpubContainer = epubContainer };
                Assert.IsNotNull(synth.PackageFileUri);
                Assert.AreEqual(synth.PackageFileUri.AbsoluteUri, synth.PackageFile.BaseUri, "Package file has wrong base uri");

            }
        }

        [DataRow(@"Epub\test.epub")]
        [DataTestMethod]
        public void XhtmlDocumentsTests(string epubFile)
        {
            epubFile = ResolvePath(epubFile);
            using (var epubContainer = ZipFile.Open(epubFile, ZipArchiveMode.Update))
            {
                var synth = new EpubSynthesizer() {EpubContainer = epubContainer};
                Assert.AreEqual(
                    synth.PackageFile?.Descendants(EpubSynthesizer.OpfNs + "item").Count(item =>
                        item.Attribute("media-type")?.Value == "application/xhtml+xml") ?? 0,
                    synth.XhtmlDocuments.Count(),
                    "Unexpected number of Xhtml documents");
            }
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
            using (var epubContainer = ZipFile.Open(output, ZipArchiveMode.Update))
            {
                var synth = new EpubSynthesizer() { EpubContainer = epubContainer };
                synth.Synthesize();
                Assert.AreEqual(
                    synth.XhtmlDocuments.Count(),
                    synth.EpubContainer.Entries.Count(entry => entry.Name.EndsWith(".smil")),
                    "Expected one smil file per xhtml file");
                Assert.AreEqual(
                    synth.XhtmlDocuments.Count(),
                    synth.EpubContainer.Entries.Count(entry => entry.Name.EndsWith(".mp3")),
                    "Expected one audio file per xhtml file");
                Assert.AreEqual(
                    synth.XhtmlDocuments.Count(),
                    synth.PackageFile.Descendants(EpubSynthesizer.OpfNs + "item").Count(item => item.Attribute("href")?.Value?.EndsWith(".mp3") ?? false),
                    "Expected one mp3 item in manifest per xhtml file");
                Assert.AreEqual(
                    synth.XhtmlDocuments.Count(),
                    synth.PackageFile.Descendants(EpubSynthesizer.OpfNs + "item").Count(item => item.Attribute("href")?.Value?.EndsWith(".smil") ?? false),
                    "Expected one smil item in manifest per xhtml file");
            }
        }
    }
}
