using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace DtbSynthesizerLibrary.Xhtml
{
    public class EpubSynthesizer
    {
        public static XNamespace OcfNs = "urn:oasis:names:tc:opendocument:xmlns:container";
        public static XNamespace OpfNs = "http://www.idpf.org/2007/opf";
        public static Uri OcfContainerUri = new Uri("zip://ocf");

        public ZipArchive EpubContainer { get; set; }

        private XDocument GetXDocument(Uri uri)
        {
            if (uri.Scheme == OcfContainerUri.Scheme || uri.Host == OcfContainerUri.Host)
            {
                return EpubContainer
                    ?.Entries
                    .Where(e => e.FullName == uri.AbsolutePath.TrimStart('/'))
                    .Select(e =>
                    {
                        using (var stream = e.Open())
                        {
                            return XDocument.Load(
                                XmlReader.Create(
                                    new StreamReader(stream),
                                    new XmlReaderSettings() { DtdProcessing = DtdProcessing.Ignore },
                                    uri.AbsoluteUri),
                                LoadOptions.SetBaseUri | LoadOptions.SetLineInfo);
                        }
                    }).Single();
            }
            return null;
        }


        public Uri PackageFileUri => new Uri(
            OcfContainerUri,
            GetXDocument(new Uri(OcfContainerUri, "META-INF/container.xml"))
                .Descendants(OcfNs + "rootfile")
                .First()
                .Attribute("full-path")?.Value ?? "");

        public XDocument PackageFile => GetXDocument(PackageFileUri);

        public IEnumerable<XDocument> XhtmlDocuments => PackageFile
            .Descendants(OpfNs+"item")
            .Where(item => item.Attribute("media-type")?.Value == "application/xhtml+xml")
            .Select(item => item.Attribute("href"))
            .Select(Utils.GetUri)
            .Select(GetXDocument);

        protected bool FireProgress(int percentage, string message)
        {
            var d = Progress;
            if (d != null)
            {
                var a = new ProgressEventArgs(percentage, message);
                d(this, a);
                return a.Cancel;
            }
            return false;
        }

        public event EventHandler<ProgressEventArgs> Progress;


    }
}
