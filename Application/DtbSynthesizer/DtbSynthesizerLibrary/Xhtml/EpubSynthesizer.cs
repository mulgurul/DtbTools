using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using NAudio.Lame;
using NAudio.Wave;

namespace DtbSynthesizerLibrary.Xhtml
{
    public class EpubSynthesizer : AbstractSynthesizer
    {
        public static XNamespace XhtmlNs => Utils.XhtmlNs;
        public static XNamespace OcfNs => Utils.OcfNs;
        public static XNamespace OpfNs => Utils.OpfNs;
        public static XNamespace DcNs => Utils.DcNs;
        public static Uri OcfContainerUri = new Uri("zip://ocf");

        private bool IsOcfContainerUri(Uri uri)
        {
            return uri?.Scheme == OcfContainerUri.Scheme && uri?.Host == OcfContainerUri.Host;
        }

        public ZipArchive EpubContainer { get; set; }
        protected Mutex EpubContainerMutex { get; } = new Mutex();

        public XDocument GetXDocument(Uri uri)
        {
            if (IsOcfContainerUri(uri))
            {
                if (!EpubContainerMutex.WaitOne())
                {
                    throw new ApplicationException("Could not get Epub Container access Mutex");
                }
                try
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
                finally
                {
                    EpubContainerMutex.ReleaseMutex();
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
            if (!EpubContainerMutex.WaitOne())
            {
                throw new ApplicationException("Could not get Epub Container access Mutex");
            }
            try
            {
                var entry = EpubContainer.Entries.FirstOrDefault(e => e.FullName == uri.AbsolutePath.TrimStart('/'));
                if (entry == null)
                {
                    throw new ArgumentException($"{uri} is not found in Ocf container", nameof(uri));
                }
                SaveXDocument(doc, entry);
            }
            finally
            {
                EpubContainerMutex.ReleaseMutex();
            }
        }

        public void AddXDocument(XDocument doc, Uri uri)
        {
            if (!IsOcfContainerUri(uri))
            {
                throw new ArgumentException($"{uri} is not rooted in Ocf container uri {OcfContainerUri}", nameof(uri));
            }
            if (!EpubContainerMutex.WaitOne())
            {
                throw new ApplicationException("Could not get Epub Container access Mutex");
            }
            try
            {
                SaveXDocument(doc, EpubContainer.CreateEntry(uri.AbsolutePath.TrimStart('/')));
            }
            finally
            {
                EpubContainerMutex.ReleaseMutex();
            }
        }

        private void SaveXDocument(XDocument doc, ZipArchiveEntry entry)
        {
            using (var stream = entry.Open())
            {
                stream.SetLength(0);
                using (var swr = new StreamWriter(stream, Encoding.GetEncoding(doc.Declaration.Encoding)))
                {
                    using (var xwr = XmlWriter.Create(swr))
                    {
                        doc.WriteTo(xwr);
                    }
                }
                stream.Close();
            }
        }

        public XElement AddWaveStreamAsMp3(Stream waveStream, Uri uri, string itemId = null)
        {
            if (waveStream == null) throw new ArgumentNullException(nameof(waveStream));
            if (uri == null) throw new ArgumentNullException(nameof(uri));
            if (!IsOcfContainerUri(uri))
            {
                throw new ArgumentException($"{uri} is not rooted in Ocf container uri {OcfContainerUri}", nameof(uri));
            }
            using (var reader = new WaveFileReader(waveStream))
            {
                if (!EpubContainerMutex.WaitOne())
                {
                    throw new ApplicationException("Could not get Epub Container access Mutex");
                }
                try
                {
                    var entry = EpubContainer.CreateEntry(uri.AbsolutePath.TrimStart('/'));
                    using (var mp3Stream = entry.Open())
                    {
                        var writer = new LameMP3FileWriter(mp3Stream, AudioWaveFormat, Mp3BitRate);
                        reader.CopyTo(writer);
                        writer.Close();
                    }
                }
                finally
                {
                    EpubContainerMutex.ReleaseMutex();
                }
            }
            var item = new XElement(
                OpfNs + "item",
                new XAttribute("href", PackageFileUri.MakeRelativeUri(uri)),
                new XAttribute("media-type", "audio/mpeg"));
            if (!String.IsNullOrEmpty(itemId))
            {
                item.SetAttributeValue("id", itemId);
            }
            return item;
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


        private XElement GetItemElement(Uri uri)
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
        int audioFileNo;
        MemoryStream waveMemoryStream;
        
        public override bool Synthesize()
        {
            var xhtmlDocs = XhtmlDocuments
                .Where(doc =>
                    String.IsNullOrEmpty(GetItemElement(new Uri(doc.BaseUri))?.Attribute("media-overlay")?.Value))
                .ToList();
            var packageFile = PackageFile;
            var totalChars = xhtmlDocs.Select(doc => doc.Root?.Value?.Length ?? 0).Sum();
            if (totalChars == 0)
            {
                return true;
            }
            var synthesizedChars = 0;
            var manifest = packageFile.Descendants(OpfNs + "manifest").Single();
            var metadata = packageFile.Descendants(OpfNs + "metadata").Single();
            var totalDur = TimeSpan.Zero;
            audioFileNo = 0;
            foreach (var doc in xhtmlDocs)
            {
                foreach (var elem in doc.Descendants(Utils.XhtmlNs + "body").Elements())
                {
                    Utils.SetMissingIds(elem);
                }
                UpdateXDocument(doc);
                var docUri = new Uri(doc.BaseUri);
                var docBaseName = Path.GetFileNameWithoutExtension(docUri.Segments.Last());
                var firstPreExistingFile = new[] {".mp3", ".smil"}
                    .Select(ext => new Uri(new Uri(doc.BaseUri), $"{docBaseName}{ext}"))
                    .FirstOrDefault(uri => EpubContainer.Entries.Any(e =>
                        e.FullName.Equals(uri.AbsolutePath.TrimStart('/'),
                            StringComparison.InvariantCultureIgnoreCase)));
                if (firstPreExistingFile != null)
                {
                    throw new ApplicationException($"Epub container already contains file {firstPreExistingFile.AbsolutePath}");
                }
                var chars = doc.Root?.Value?.Length ?? 0;
                var synth = new EpubXhtmlSynthesizer()
                {
                    XhtmlDocument = doc,
                    AudioFileSrc = $"{docBaseName}.mp3",
                    AudioWaveFormat = AudioWaveFormat,
                    Mp3BitRate = Mp3BitRate,
                    DefaultSynthesizer = DefaultSynthesizer,
                    SynthesizerSelector = SynthesizerSelector
                };
                synth.Progress += (sender, args) =>
                {
                    if (FireProgress(
                        100 * (synthesizedChars + (args.ProgressPercentage*chars/100)) / totalChars,
                        $"Synthesizing ePub xhtml file {doc.BaseUri}"))
                    {
                        args.Cancel = true;
                    }
                };
                var smilUri = new Uri(docUri, $"{docBaseName}.smil");
                waveMemoryStream = new MemoryStream();
                var mp3Uri = new Uri(PackageFileUri, $"{AudioFilePrefix}{audioFileNo++:D5}.mp3");
                synth.AudioWriter = new WaveFileWriter(waveMemoryStream, AudioWaveFormat);
                synth.AudioFileSrc = smilUri.MakeRelativeUri(mp3Uri).ToString();
                synth.ElementReached += (sender, args) =>
                {
                    if (
                        synth.HeaderNames.Contains(args.Element.Name) 
                        && args
                            .Element
                            .XPathSelectElements("preceding::*")
                            .Any(e => synth.HeaderNames.Contains(e.Name)))
                    {
                        waveMemoryStream.Position = 0;
                        manifest.Add(AddWaveStreamAsMp3(waveMemoryStream, mp3Uri, Utils.GenerateNewId(packageFile)));
                        waveMemoryStream.SetLength(0);
                        mp3Uri = new Uri(PackageFileUri, $"{AudioFilePrefix}{audioFileNo++:D5}.mp3");
                        synth.AudioWriter = new WaveFileWriter(waveMemoryStream, AudioWaveFormat);
                        synth.AudioFileSrc = smilUri.MakeRelativeUri(mp3Uri).ToString();
                    }
                };
                if (!synth.Synthesize())
                {
                    return false;
                }
                waveMemoryStream.Position = 0;
                manifest.Add(AddWaveStreamAsMp3(waveMemoryStream, mp3Uri, Utils.GenerateNewId(packageFile)));
                AddXDocument(synth.MediaOverlayDocument, smilUri);
                var smilId = Utils.GenerateNewId(packageFile);
                manifest.Add(new XElement(
                    OpfNs + "item",
                    new XAttribute("id", smilId),
                    new XAttribute("href", PackageFileUri.MakeRelativeUri(smilUri)),
                    new XAttribute("media-type", "application/smil+xml")));
                var xhtmlItem = manifest.Elements(OpfNs + "item").Single(item =>
                    (item.Attribute("href")?.Value??"(") == PackageFileUri.MakeRelativeUri(docUri).ToString());
                xhtmlItem.SetAttributeValue("media-overlay", smilId);
                var dur = TimeSpan.FromSeconds(
                    synth
                        .MediaOverlayDocument
                        .Descendants(EpubXhtmlSynthesizer.Smil30Ns + "audio")
                        .Select(Utils.GetAudioDuration)
                        .Sum(ts => ts.TotalSeconds));
                metadata.Add(new XElement(
                    OpfNs+"meta",
                    new XAttribute("property", "media:duration"),
                    new XAttribute("refines", $"#{smilId}"),
                    Utils.GetHHMMSSFromTimeSpan(dur)));
                totalDur += dur;
            }
            metadata.Add(new XElement(
                OpfNs + "meta",
                new XAttribute("property", "media:duration"),
                Utils.GetHHMMSSFromTimeSpan(totalDur)));
            UpdateXDocument(packageFile);
            return true;
        }
    }
}
