using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using DtbSynthesizerLibrary.Xml;
using NAudio.Lame;
using NAudio.Wave;

namespace DtbSynthesizerLibrary.Xhtml
{
    public class Daisy202Synthesizer : AbstractXhtmlSynthesizer
    {
        private int audioFileNumber;

        private string AudioFileName => $"{AudioFilePrefix}{audioFileNumber:D5}.{(EncodeMp3 ? "mp3" : "wav")}";
        private string AudioFilePath => Path.Combine(OutputDirectory, AudioFileName);

        public override bool Synthesize()
        {
            ValidateSynthesizer();
            audioFileNumber = -1;
            WaveFileWriter writer = null;
            MemoryStream writerStream = null;
            var encodingTaskStack = new Stack<Task>();
            var elements = ElementsToSynthesize;
            for (int i = 0; i < elements.Count; i++)
            {
                if (FireProgress(
                    100 * i / elements.Count,
                    $"Synthesizing element {i + 1} of {elements.Count} to {AudioFileName}"))
                {
                    return false;
                }
                var elem = elements[i];
                if (HeaderNames.Contains(elem.Name) || writer == null)
                {
                    if (EncodeMp3)
                    {
                        writer?.Flush();
                        encodingTaskStack.Push(EncodeMp3AudioFile(writerStream, AudioFilePath));
                        audioFileNumber++;
                        writerStream = new MemoryStream();
                        writer = new WaveFileWriter(writerStream, AudioWaveFormat);
                    }
                    else
                    {
                        writer?.Close();
                        audioFileNumber++;
                        writer = new WaveFileWriter(AudioFilePath, AudioWaveFormat);
                    }
                }
                var ci = Utils.SelectCulture(elem);
                var synth = CultureInfo.InvariantCulture.Equals(ci)
                    ? DefaultSynthesizer
                    : SynthesizerSelector(ci);
                synth.TextToSynthesizeDelegate = e => e.Value;
                synth.SynthesizeElement(elem, writer, AudioFileName);
            }
            if (EncodeMp3 && writer != null)
            {
                writer.Flush();
                encodingTaskStack.Push(EncodeMp3AudioFile(writerStream, AudioFilePath));
                var taskCount = encodingTaskStack.Count;
                while (encodingTaskStack.Any())
                {
                    FireProgress(
                        100 * (taskCount - encodingTaskStack.Count) / taskCount,
                        $"Waiting for mp3 encoding to finish {encodingTaskStack.Count} left of {taskCount}");
                    encodingTaskStack.Pop().Wait();
                }
            }
            else
            {
                writer?.Close();
            }
            return true;
        }


        private readonly IDictionary<string, XDocument> smilFilesByFileName = new Dictionary<string, XDocument>();

        public IReadOnlyDictionary<string, XDocument> SmilFilesByName =>
            new ReadOnlyDictionary<string, XDocument>(smilFilesByFileName);

        public string DcIdentifier
        {
            get => Utils.GetMetaContent(XhtmlDocument, "dc:identifier");

            set
            {
                if (value == null)
                {
                    throw new InvalidOperationException($"Cannot set {nameof(DcIdentifier)} to null");
                }

                if (XhtmlDocument == null)
                {
                    throw new InvalidOperationException($"Cannot set {nameof(DcIdentifier)} when {nameof(XhtmlDocument)} is null");
                }
                Utils.SetMeta(XhtmlDocument, "dc:identifier", value);
            }
        }

        public string OutputDirectory => Path.GetDirectoryName(XhtmlPath) ?? Directory.GetCurrentDirectory();
        public bool EncodeMp3 { get; set; } = true;

        public string XhtmlPath
        {
            get
            {
                var baseUri = XhtmlDocument?.BaseUri;
                if (baseUri == null)
                {
                    return null;
                }
                try
                {
                    return new Uri(baseUri).LocalPath;
                }
                catch (Exception e)
                {
                    throw new InvalidOperationException(
                        $"Could not get local path from {nameof(XhtmlDocument)} BaseUri {baseUri}: {e.Message}");
                }
            }
        }

        public XDocument NccDocument;

        public bool GenerateDtb()
        {
            NormalizeSpaceInXhtmlDocument(XhtmlDocument);
            Utils.SetMeta(XhtmlDocument, "dc:format", "Daisy 2.02");
            MovePageNumbers();
            if (!Synthesize()) return false;
            if (!GenerateSmilFiles()) return false;
            int i = 0;
            foreach (var smilFileName in SmilFilesByName.Keys)
            {
                if (FireProgress(100 * i / SmilFilesByName.Count,
                    $"Saving smil file {smilFileName} ({i + 1}/{SmilFilesByName.Count}"))
                {
                    return false;
                }
                SmilFilesByName[smilFileName].Save(Path.Combine(OutputDirectory, smilFileName));
                i++;
            }
            GenerateNccDocument();
            NccDocument.Save(Path.Combine(OutputDirectory, "ncc.html"));
            XhtmlDocument.Save(XhtmlPath);
            return true;
        }

        public bool GenerateSmilFiles()
        {
            var xhtmlFileName = Path.GetFileName(XhtmlPath);
            var smilFiles = new Dictionary<string, XDocument>();
            var annos = (Body ?? new XElement(XhtmlNs + "body"))
                .DescendantNodes()
                .SelectMany(n => n.Annotations<SyncAnnotation>())
                .Where(anno =>
                    (anno.Src?.EndsWith(EncodeMp3 ? ".mp3" : ".wav") ?? false)
                    && !String.IsNullOrEmpty(anno.Element?.Attribute("id")?.Value)
                    && anno.ClipEnd > anno.ClipBegin)
                .ToList();
            for (int i = 0; i < annos.Count; i++)
            {
                if (FireProgress(
                    100 * i / annos.Count,
                    $"Handling synchronization point {i + 1} of {annos.Count} to {AudioFileName}"))
                {
                    return false;
                }
                var anno = annos[i];
                var smilName = $"{anno.Src.Substring(0, anno.Src.Length - 4)}.smil".ToLowerInvariant();
                if (!smilFiles.ContainsKey(smilName))
                {
                    smilFiles.Add(smilName,
                        Utils.GenerateSkeletonDaisy202SmilDocument(new Uri(new Uri(XhtmlDocument.BaseUri), smilName)
                            .AbsoluteUri));
                }
                var smilFile = smilFiles[smilName];
                var mainSeq = smilFile.Root?.Elements("body").SelectMany(body => body.Elements("seq")).FirstOrDefault();
                if (mainSeq == null)
                {
                    throw new ApplicationException($"Smil file {smilName} contains no main seq");
                }
                var textReference = $"{xhtmlFileName}#{anno.Element?.Attribute("id")?.Value}";
                var lastTextReference = mainSeq
                    .Elements("par")
                    .LastOrDefault()
                    ?.Element("text")
                    ?.Attribute("src")
                    ?.Value;
                var audio = new XElement(
                    "audio",

                    new XAttribute(
                        "src",
                        anno.Src),
                    new XAttribute(
                        "clip-begin",
                        $"npt={anno.ClipBegin.TotalSeconds.ToString("F3", CultureInfo.InvariantCulture)}s"),
                    new XAttribute(
                        "clip-end",
                        $"npt={anno.ClipEnd.TotalSeconds.ToString("F3", CultureInfo.InvariantCulture)}s"));
                XElement par;
                if (textReference.Equals(lastTextReference, StringComparison.InvariantCulture))
                {
                    par = mainSeq.Elements("par").Last();
                    var seq = par.Element("seq");
                    if (seq == null)
                    {
                        var a = par.Elements("audio").Single();
                        a.Remove();
                        seq = new XElement("seq", a);
                        par.Add(seq);
                    }
                    seq.Add(audio);
                }
                else
                {
                    par = new XElement(
                        "par",
                        new XAttribute("id", $"par_{mainSeq.Elements("par").Count():D5}"),
                        new XAttribute("endsync", "last"),
                        new XElement(
                            "text",
                            new XAttribute("src", textReference),
                            new XAttribute("id", $"text_{mainSeq.Elements("par").Count():D5}")),
                        audio);
                    mainSeq.Add(par);
                }
                audio.SetAttributeValue("id",
                    $"audio_{par.ElementsBeforeSelf("par").Count():D5}_{par.Descendants("audio").Count():D5}");
            }

            var totalElapsedTime = TimeSpan.Zero;
            foreach (var kvp in smilFiles.OrderBy(o => o.Key))
            {
                var audioFile = $"{kvp.Key.Substring(0, kvp.Key.Length - 5)}{(EncodeMp3 ? ".mp3" : ".wav")}";
                var timeInThisSmil = TimeSpan.FromMilliseconds(
                    Body
                        ?.DescendantNodes()
                        .SelectMany(n => n.Annotations<SyncAnnotation>())
                        .Where(anno => audioFile.Equals(anno.Src, StringComparison.InvariantCultureIgnoreCase))
                        .Sum(anno => anno.ClipEnd.Subtract(anno.ClipBegin).TotalMilliseconds)
                    ??0.0);
                Utils.SetMeta(kvp.Value, "ncc:totalElapsedTime", Utils.GetHHMMSSFromTimeSpan(totalElapsedTime));
                Utils.SetMeta(kvp.Value, "ncc:timeInThisSmil", Utils.GetHHMMSSFromTimeSpan(timeInThisSmil));
                Utils.SetMeta(kvp.Value, "ncc:generator", Utils.Generator);
                Utils.SetMeta(kvp.Value, "dc:identifier", DcIdentifier);
                kvp.Value.Root?.Element("body")?.Element("seq")?.SetAttributeValue(
                    "dur", 
                    $"{timeInThisSmil.TotalSeconds.ToString("f3", CultureInfo.InvariantCulture)}s");

                totalElapsedTime += timeInThisSmil;
            }

            smilFilesByFileName.Clear();
            foreach (var kvp in smilFiles)
            {
                smilFilesByFileName.Add(kvp);
            }
            return true;
        }

        public void GenerateNccDocument()
        {
            var ncc = Utils.GenerateSkeletonXhtmlDocument(new Uri(new Uri(XhtmlDocument.BaseUri), "ncc.html").AbsoluteUri);
            ncc.Root?.Element(XhtmlNs + "head")?.Add(XhtmlDocument.Root?.Element(XhtmlNs + "head")?.Elements());
            var nsMgr = new XmlNamespaceManager(ncc.CreateReader().NameTable??new NameTable());
            nsMgr.AddNamespace("x", XhtmlNs.NamespaceName);
            var nccBody = ncc.XPathSelectElements("/x:html/x:body", nsMgr).Single();
            if (Utils.SelectCulture(Body).IsNeutralCulture)
            foreach (var kvp in SmilFilesByName)
            {
                var smilTextSrcAttrs = kvp
                    .Value
                    .Descendants("text")
                    .Select(text => text.Attribute("src"))
                    .Where(src => !String.IsNullOrEmpty(src?.Value))
                    .ToList();
                var firstSrcId = smilTextSrcAttrs.First().Value.Split('#').Last();
                var heading = new XElement(Body.Descendants().First(e => e.Attribute("id")?.Value == firstSrcId));
                var a = new XElement(
                    XhtmlNs+"a",
                    new XAttribute("href", $"{kvp.Key}#par_00000"));
                foreach (var e in heading.Nodes().ToList())
                {
                    e.Remove();
                    a.Add(e);
                }
                heading.Add(a);
                nccBody.Add(heading);
                var pageSpans = Body
                    .Descendants(XhtmlNs + "span")
                    .Where(IsPageNumberElement)
                    .Where(span => smilTextSrcAttrs.Any(src => src.Value.Split('#').Last().Equals(span.Attribute("id")?.Value)))
                    .Select(span => new XElement(span));
                foreach (var pageSpan in pageSpans)
                {
                    var smilParId = smilTextSrcAttrs
                        .Single(src => src.Value.Split('#').Last().Equals(pageSpan.Attribute("id")?.Value)).Parent?.Parent?.Attribute("id")?.Value;
                    a = new XElement(
                        XhtmlNs + "a",
                        new XAttribute("href", $"{kvp.Key}#{smilParId}"));
                    foreach (var n in pageSpan.Nodes().ToList())
                    {
                        n.Remove();
                        a.Add(n);
                    }
                    pageSpan.Add(a);
                    nccBody.Add(pageSpan);
                }
            }
            Utils.SetMeta(ncc, "dc:format", "Daisy 2.02");
            var imageFileCount = Body
                .Descendants(XhtmlNs + "img")
                .Select(img => img.Attribute("src")?.Value.ToLowerInvariant())
                .Distinct()
                .Count(src => Uri.IsWellFormedUriString(src, UriKind.Relative));
            Utils.SetMeta(
                ncc,
                "ncc:files",
                (2 + SmilFilesByName.Count + AudioFiles.Count() + imageFileCount).ToString());
            Utils.SetMeta(
                ncc, 
                "ncc:totalTime",
                Utils.GetHHMMSSFromTimeSpan(
                    TimeSpan.FromSeconds(
                        Body.DescendantNodes().SelectMany(n =>
                            n.Annotations<SyncAnnotation>().Select(anno => (anno.ClipEnd - anno.ClipBegin).TotalSeconds)).Sum())));
            Utils.SetMeta(ncc, "ncc:tocItems", nccBody.Elements().Count().ToString());
            Utils.SetMeta(
                ncc, 
                "ncc:depth", 
                Enumerable.Range(1, 6).Where(i => nccBody.Elements(XhtmlNs + $"h{i}").Any()).Max().ToString());
            var nar = (SynthesizerSelector(Utils.SelectCulture(nccBody))??DefaultSynthesizer)?.VoiceInfo.Name;
            if (!String.IsNullOrEmpty(nar))
            {
                Utils.SetMeta(ncc, "ncc:narrator", nar);
            }
            Utils.SetMeta(ncc, "ncc:multimediaType", "audioFullText");
            Utils.SetMeta(ncc, "ncc:generator", Utils.Generator);
            Utils.SetMeta(ncc, "ncc:charset", "utf-8");
            foreach (var type in new[] {"Front", "Normal", "Special"})
            {
                Utils.SetMeta(
                    ncc,
                    $"ncc:page{type}",
                    nccBody
                        .Elements(XhtmlNs + "span")
                        .Count(span => $"page-{type.ToLowerInvariant()}".Equals(span.Attribute("class")?.Value))
                        .ToString());
            }
            Utils.SetMeta(
                ncc,
                "ncc:maxPageNormal",
                nccBody
                    .Elements(XhtmlNs + "span")
                    .Where(span => "page-normal".Equals(span.Attribute("class")?.Value))
                    .Select(span => Int32.TryParse(span.Value, out var pageNo) ? pageNo : 0)
                    .Union(new[]{0})
                    .Max()
                    .ToString());
            var bodyLang = Utils.GetLanguage(Body);
            if (String.IsNullOrWhiteSpace(Utils.GetMetaContent(ncc, "dc:language")) &&
                !String.IsNullOrWhiteSpace(bodyLang))
            {
                Utils.SetMeta(ncc, "dc:language", bodyLang);
            }
            var title = 
                Utils.GetMetaContent(ncc, "dc:title") 
                ?? Body.Descendants(XhtmlNs + "h1").FirstOrDefault()?.Value 
                ?? "";
            var titleElement = ncc
                .Elements(XhtmlNs + "html").Single()
                .Elements(XhtmlNs + "head").Single()
                .Elements(XhtmlNs + "title").Single();
            if (String.IsNullOrWhiteSpace(titleElement.Value))
            {
                titleElement.Value = title;
            }
            if (String.IsNullOrWhiteSpace(Utils.GetMetaContent(ncc, "dc:title")))
            {
                Utils.SetMeta(ncc, "dc:title", title);
            }
            if (String.IsNullOrWhiteSpace(Utils.GetMetaContent(ncc, "dc:date")))
            {
                Utils.SetMeta(ncc, "dc:date", DateTime.Today.ToString("yyyy-MM-dd")).SetAttributeValue("scheme", "yyyy-mm-dd");
            }
            NormalizeSpaceInXhtmlDocument(ncc);
            NccDocument = ncc;
        }

        protected async Task EncodeMp3AudioFile(Stream waveStream, string audioFilePath)
        {
            if (waveStream == null)
            {
                return;
            }
            waveStream.Position = 0;
            using (var reader = new WaveFileReader(waveStream))
            {
                using (var mp3Writer = new LameMP3FileWriter(
                    audioFilePath,
                    AudioWaveFormat,
                    Mp3BitRate))
                {
                    await reader.CopyToAsync(mp3Writer);
                }
            }
            waveStream.Dispose();
        }
    }
}
