using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;
using DtbSynthesizerLibrary.Xhtml;
using NAudio.Lame;
using NAudio.Wave;

namespace DtbSynthesizerLibrary.Epub
{
    public class EpubSynthesizer : AbstractSynthesizer
    {
        private static XNamespace OpfNs => Utils.OpfNs;

        public EpubPublication Publication { get; set; }

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

        public XElement AddWaveStreamAsMp3(Stream waveStream, Uri uri, string itemId = null)
        {
            if (waveStream == null) throw new ArgumentNullException(nameof(waveStream));
            using (var reader = new WaveFileReader(waveStream))
            {
                using (var mp3Stream = Publication.CreateContainerStream(uri))
                {
                    var writer = new LameMP3FileWriter(mp3Stream, AudioWaveFormat, Mp3BitRate);
                    reader.CopyTo(writer);
                    writer.Close();

                }
            }
            var item = new XElement(
                OpfNs + "item",
                new XAttribute("href", Publication.PackageFileUri.MakeRelativeUri(uri)),
                new XAttribute("media-type", "audio/mpeg"));
            if (!String.IsNullOrEmpty(itemId))
            {
                item.SetAttributeValue("id", itemId);
            }
            return item;
        }


        int audioFileNo;
        MemoryStream waveMemoryStream;
        
        public override bool Synthesize()
        {
            var xhtmlDocs = Publication.XhtmlDocuments
                .Where(doc =>
                    String.IsNullOrEmpty(Publication.GetItemElement(new Uri(doc.BaseUri))?.Attribute("media-overlay")?.Value))
                .ToList();
            var packageFile = Publication.PackageFile;
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
                Publication.UpdateXDocument(doc);
                var docUri = new Uri(doc.BaseUri);
                var docBaseName = Path.GetFileNameWithoutExtension(docUri.Segments.Last());
                var firstPreExistingFile = new[] {".mp3", ".smil"}
                    .Select(ext => new Uri(new Uri(doc.BaseUri), $"{docBaseName}{ext}"))
                    .FirstOrDefault(uri => Publication.ContainsFile(uri, true));
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
                var mp3Uri = new Uri(Publication.PackageFileUri, $"{AudioFilePrefix}{audioFileNo++:D5}.mp3");
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
                        manifest.Add(AddWaveStreamAsMp3(
                            waveMemoryStream, 
                            mp3Uri, 
                            Utils.GenerateNewId(packageFile)));
                        waveMemoryStream.SetLength(0);
                        mp3Uri = new Uri(Publication.PackageFileUri, $"{AudioFilePrefix}{audioFileNo++:D5}.mp3");
                        synth.AudioWriter = new WaveFileWriter(waveMemoryStream, AudioWaveFormat);
                        synth.AudioFileSrc = smilUri.MakeRelativeUri(mp3Uri).ToString();
                    }
                };
                if (!synth.Synthesize())
                {
                    return false;
                }
                waveMemoryStream.Position = 0;
                manifest.Add(AddWaveStreamAsMp3(
                    waveMemoryStream, 
                    mp3Uri,
                    Utils.GenerateNewId(packageFile)));
                Publication.AddXDocument(synth.MediaOverlayDocument, smilUri);
                var smilId = Utils.GenerateNewId(packageFile);
                manifest.Add(new XElement(
                    OpfNs + "item",
                    new XAttribute("id", smilId),
                    new XAttribute("href", Publication.PackageFileUri.MakeRelativeUri(smilUri)),
                    new XAttribute("media-type", "application/smil+xml")));
                var xhtmlItem = manifest.Elements(OpfNs + "item").Single(item =>
                    (item.Attribute("href")?.Value??"(") == Publication.PackageFileUri.MakeRelativeUri(docUri).ToString());
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
            Publication.UpdateXDocument(packageFile);
            return true;
        }
    }
}
