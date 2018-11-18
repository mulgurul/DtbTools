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
    public class EpubPublicationTests
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
            using (var epubPublication = new EpubPublication() {Path = epubFile})
            {
                Assert.IsNotNull(epubPublication.PackageFileUri);
                Assert.AreEqual(epubPublication.PackageFileUri.AbsoluteUri, epubPublication.PackageFile.BaseUri, "Package file has wrong base uri");
            }
        }

        [DataRow(@"Epub\test.epub")]
        [DataTestMethod]
        public void XhtmlDocumentsTests(string epubFile)
        {
            epubFile = ResolvePath(epubFile);
            using (var epubPublication = new EpubPublication(){Path = epubFile})
            {
                Assert.AreEqual(
                    epubPublication.PackageFile?.Descendants(Utils.OpfNs + "item").Count(item =>
                        item.Attribute("media-type")?.Value == "application/xhtml+xml") ?? 0,
                    epubPublication.XhtmlDocuments.Count(),
                    "Unexpected number of Xhtml documents");
                Assert.AreEqual(
                    epubPublication.PackageFile
                        ?.Descendants(Utils.OpfNs + "item")
                        .Where(item =>
                            (item.Attribute("media-type")?.Value ?? "") == "application/xhtml+xml"
                            && (item.Attribute("properties")?.Value ?? "") == "nav")
                        .Select(item => new Uri(epubPublication.PackageFileUri, item.Attribute("href")?.Value ?? ""))
                        .Single(),
                    new Uri(epubPublication.XhtmlDocuments.First().BaseUri),
                    "First XhtmlDocument is not nav document");
            }
        }
    }
}
