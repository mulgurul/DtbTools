using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using DtbSynthesizerLibrary.Xhtml;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DtbSynthesizerLibraryTests.Xhtml
{
    [TestClass]
    public class EpubSynthesizerTests
    {
        public TestContext TestContext { get; set; }

        [DataRow(@"~\Documents\718230.epub")]
        [DataTestMethod]
        public void PackageFileTests(string epubFile)
        {
            if (epubFile.StartsWith(@"~\"))
            {
                epubFile = Path.Combine(Environment.GetEnvironmentVariable("USERPROFILE")??@".\", epubFile.Substring(2));
            }
            using (var epubContainer = ZipFile.Open(epubFile, ZipArchiveMode.Update))
            {
                var synth = new EpubSynthesizer() { EpubContainer = epubContainer };
                Assert.IsNotNull(synth.PackageFileUri);
                Assert.AreEqual(synth.PackageFileUri.AbsoluteUri, synth.PackageFile.BaseUri, "Package file has wrong base uri");

            }
        }

        [DataRow(@"~\Documents\718230.epub")]
        [DataTestMethod]
        public void XhtmlDocumentsTests(string epubFile)
        {
            if (epubFile.StartsWith(@"~\"))
            {
                epubFile = Path.Combine(Environment.GetEnvironmentVariable("USERPROFILE") ?? @".\", epubFile.Substring(2));
            }
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
    }
}
