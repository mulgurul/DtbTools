using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml;
using System.Xml.Linq;

namespace DtbSynthesizerLibrary.Epub
{
    public class EpubPublication : IDisposable
    {

        private static XNamespace OcfNs => Utils.OcfNs;
        private static XNamespace OpfNs => Utils.OpfNs;
        private static XNamespace DcNs => Utils.DcNs;
        public static Uri OcfContainerUri = new Uri("zip://ocf");
        public static bool IsOcfContainerUri(Uri uri)
        {
            return uri?.Scheme == OcfContainerUri.Scheme && uri?.Host == OcfContainerUri.Host;
        }

        private string path;

        public string Path
        {
            get => path;
            set
            {
                if (path != null && path.Equals(value))
                {
                    return;
                }
                if (!EpubContainerMutex.WaitOne())
                {
                    throw new ApplicationException("Could not get EpubContainer Mutex");
                }
                try
                {
                    EpubContainer?.Dispose();
                    path = value;
                    if (path != null)
                    {
                        EpubContainer = ZipFile.Open(path, ZipArchiveMode.Update);
                    }
                }
                finally
                {
                    EpubContainerMutex.ReleaseMutex();
                }
            }
        }

        public IEnumerable<Uri> FileUris => EpubContainer?.Entries.Select(entry => new Uri(OcfContainerUri, entry.FullName)) ?? new Uri[0];

        public bool ContainsFile(Uri uri, bool ignoreCase = true)
        {
            if (uri == null) throw new ArgumentNullException(nameof(uri));
            if (ignoreCase)
            {
                return FileUris.Any(fu =>
                    fu.AbsoluteUri.Equals(uri.AbsoluteUri, StringComparison.InvariantCultureIgnoreCase));
            }
            return FileUris.Contains(uri);
        }

        protected ZipArchive EpubContainer { get; private set; }

        protected Mutex EpubContainerMutex { get; } = new Mutex();
        public XDocument GetXDocument(Uri uri)
        {
            if (FileUris.Contains(uri))
            {
                using (var stream = OpenContainerStream(uri))
                {
                    return XDocument.Load(
                        XmlReader.Create(
                            new StreamReader(stream),
                            new XmlReaderSettings() {DtdProcessing = DtdProcessing.Ignore},
                            uri.AbsoluteUri),
                        LoadOptions.SetBaseUri | LoadOptions.SetLineInfo);
                }
            }
            return null;
        }

        public void UpdateXDocument(XDocument doc)
        {
            var uri = new Uri(doc.BaseUri);
            UpdateXDocument(doc, uri);
        }

        public void UpdateXDocument(XDocument doc, Uri uri)
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));
            if (uri == null) throw new ArgumentNullException(nameof(uri));
            if (!FileUris.Contains(uri))
            {
                throw new ArgumentException($"{uri} is not found in Ocf container", nameof(uri));
            }
            using (var stream = OpenContainerStream(uri))
            {
                stream.SetLength(0);
                SaveXDocument(stream, doc);
            }
        }

        public void AddXDocument(XDocument doc, Uri uri)
        {
            if (!IsOcfContainerUri(uri))
            {
                throw new ArgumentException($"{uri} is not rooted in Ocf container uri {OcfContainerUri}", nameof(uri));
            }
            using (var stream = CreateContainerStream(uri))
            {
                SaveXDocument(stream, doc);
            }
        }

        private void SaveXDocument(Stream stream, XDocument doc)
        {
            using (var swr = new StreamWriter(stream, Encoding.GetEncoding(doc.Declaration?.Encoding ?? "utf-8")))
            {
                using (var xwr = XmlWriter.Create(swr, new XmlWriterSettings() {Indent = true, IndentChars = "  "}))
                {
                    doc.WriteTo(xwr);
                }
            }
        }

        public Stream CreateContainerStream(Uri uri)
        {
            if (uri == null) throw new ArgumentNullException(nameof(uri));
            if (!IsOcfContainerUri(uri))
            {
                throw new ArgumentException($"{uri} is not rooted in Ocf container uri {OcfContainerUri}", nameof(uri));
            }
            return OpenContainerStream(EpubContainer.CreateEntry(uri.AbsolutePath.TrimStart('/')));
        }

        public Stream OpenContainerStream(ZipArchiveEntry entry)
        {
            if (entry == null) throw new ArgumentNullException(nameof(entry));
            if (!EpubContainerMutex.WaitOne())
            {
                throw new ApplicationException("Could not get Epub Container access Mutex");
            }
            var result = new DisposeNotifyingStream(entry.Open());
            result.Disposed += (sender, args) => EpubContainerMutex.ReleaseMutex();
            return result;

        }

        public Stream OpenContainerStream(Uri uri)
        {
            if (uri == null) throw new ArgumentNullException(nameof(uri));
            if (!IsOcfContainerUri(uri))
            {
                throw new ArgumentException($"{uri} is not rooted in Ocf container uri {OcfContainerUri}", nameof(uri));
            }
            var entry = EpubContainer.GetEntry(uri.AbsolutePath.TrimStart('/'));
            if (entry == null) return null;
            return OpenContainerStream(entry);
        }

        public Uri PackageFileUri => new Uri(
            OcfContainerUri,
            GetXDocument(new Uri(OcfContainerUri, "META-INF/container.xml"))
                .Descendants(OcfNs + "rootfile")
                .First()
                .Attribute("full-path")?.Value ?? "");

        public XDocument PackageFile => GetXDocument(PackageFileUri);

        public CultureInfo PublicationLanguage => PackageFile
                                                      ?.Descendants(DcNs + "language")
                                                      .Select(le => new CultureInfo(le.Value))
                                                      .FirstOrDefault() 
                                                  ?? CultureInfo.InvariantCulture;

        public void SetDcIdentifier(string identifier)
        {
            var packageFile = PackageFile;
            var dcIdentifier = Utils.GetEpubUniqueIdentifierElement(packageFile, true);
            dcIdentifier.Value = identifier;
            UpdateXDocument(packageFile);
            foreach (var xhtml in XhtmlDocuments)
            {
                Utils.SetMeta(xhtml, "dc:identifier", identifier);
                UpdateXDocument(xhtml);
            }
        }

        public void SetMetadata(string name, string value)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));
            if (value == null)
            {
                value = "";
            }
            var metadata = PackageFile.Root?.Element(OpfNs+"metadata");
            if (metadata == null)
            {
                throw new InvalidOperationException("PackageFile contains no metadata element");
            }
            if (name.StartsWith("dc:"))
            {
                var elem = metadata.Element(DcNs + name.Substring(3));
                if (elem == null)
                {
                    elem = new XElement(DcNs + name.Substring(3));
                    metadata.Add(elem);
                }
                elem.Value = value;
            }
            else
            {
                var meta = metadata
                    .Elements(OpfNs + "meta")
                    .FirstOrDefault(m =>name.Equals(m.Attribute("property")?.Value, StringComparison.InvariantCulture));
                if (meta == null)
                {
                    meta = new XElement(OpfNs+"meta", new XAttribute("property", name));
                    metadata.Add(meta);
                }
                meta.Value = value;
                meta = metadata
                    .Elements(OpfNs + "meta")
                    .FirstOrDefault(m => name.Equals(m.Attribute("name")?.Value, StringComparison.InvariantCulture));
                if (meta == null)
                {
                    meta = new XElement(OpfNs + "meta", new XAttribute("name", name));
                    metadata.Add(meta);
                }
                meta.SetAttributeValue("content", value);
            }
            UpdateXDocument(metadata.Document);
        }


        public XElement GetItemElement(Uri uri)
        {
            return PackageFile
                .Descendants(OpfNs + "item")
                .SingleOrDefault(item => new Uri(PackageFileUri, item.Attribute("href")?.Value ?? "") == uri);
        }

        public IEnumerable<XDocument> XhtmlDocuments => PackageFile
            .Descendants(OpfNs+"item")
            .Where(item => item.Attribute("media-type")?.Value == "application/xhtml+xml")
            .OrderBy(item => (item.Attribute("properties")?.Value??"")=="nav"?0:1)
            .Select(item => item.Attribute("href"))
            .Select(Utils.GetUri)
            .Select(GetXDocument);

        public void Dispose()
        {
            EpubContainer?.Dispose();
            EpubContainerMutex?.Dispose();
        }
    }
}
