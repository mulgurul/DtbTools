using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
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
    public class XhtmlSynthesizer
    {

        public static XNamespace XhtmlNs => Utils.XhtmlNs;

        public static bool IsPageNumberElement(XElement element)
        {
            return
                element.Name == XhtmlNs + "span"
                && (element
                        .Attribute("class")?.Value.Split(' ')
                        .Any(cl => new[] {"page-front", "page-normal", "page-special"}.Contains(cl)) ?? false);
        }

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

        public XDocument XhtmlDocument { get; set; }

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

        public bool EncodeMp3 { get; set; } = true;

        public int Mp3BitRate { get; set; } = 32;

        public XElement Body => XhtmlDocument?.Root?.Element(XhtmlNs + "body");

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

        public string OutputDirectory => Path.GetDirectoryName(XhtmlPath) ?? Directory.GetCurrentDirectory();

        private readonly IDictionary<string, XDocument> smilFilesByFileName = new Dictionary<string, XDocument>();

        public IReadOnlyDictionary<string, XDocument> SmilFiles =>
            new ReadOnlyDictionary<string, XDocument>(smilFilesByFileName);

        public XDocument NccDocument;

        public Func<CultureInfo, IXmlSynthesizer> SynthesizerSelector { get; set; } 
            = (ci) => Utils.GetPrefferedXmlSynthesizerForCulture(ci);

        public IXmlSynthesizer DefaultSynthesizer { get; set; }

        public WaveFormat AudioWaveFormat { get; set; } = new WaveFormat(22050, 1);

        private void ValidateSynthesizer()
        {
            if (XhtmlDocument == null)
            {
                throw new InvalidOperationException($"{nameof(XhtmlDocument)} property is null");
            }
            if (XhtmlDocument.BaseUri == null)
            {
                throw new InvalidOperationException($"{nameof(XhtmlDocument)} has no BaseUri");
            }
            if (Body == null)
            {
                throw new InvalidOperationException($"{nameof(XhtmlDocument)} has no body element");
            }
            if (Body.Descendants().Any(elem => Utils.SelectCulture(elem) == null) && DefaultSynthesizer == null)
            {
                throw new InvalidOperationException($"nameof{DefaultSynthesizer} is null");
            }
            var languages = Body.Descendants().Select(Utils.GetLanguage).Distinct().Where(lang => lang != null).ToList();
            if (languages.Any())
            {
                if (SynthesizerSelector == null)
                {
                    throw new InvalidOperationException($"{nameof(SynthesizerSelector)} is null");
                }
                var langsWithNoSynth = languages.Where(lang => SynthesizerSelector(new CultureInfo(lang)) == null)
                    .ToList();
                if (langsWithNoSynth.Any())
                {
                    var ls = langsWithNoSynth.Aggregate((s, v) => $"{s};{v}");
                    throw new InvalidOperationException($"No synthesizeres found for languages {ls}");
                }
            }
            if (
                Body.Descendants().Select(Utils.SelectCulture).Any(CultureInfo.InvariantCulture.Equals) 
                && DefaultSynthesizer == null)
            {
                throw new InvalidOperationException($"{nameof(DefaultSynthesizer)} is null");
            }
        }

        public IList<XName> BlockContainerNames { get; } = new List<XName>(new[]
        {
            "article",
            "aside",
            "div",
            "footer",
            "header",
            "main",
            "section"
        }.Select(n => XhtmlNs + n));

        public IList<XName> InlineElementNames { get; } = new List<XName>(new []
        {
            "a",
            "abbr",
            "acronym",
            "b",
            "bdo",
            "big",
            "br",
            "button",
            "cite",
            "code",
            "del",
            "dfn",
            "em",
            "i",
            "img",
            "ins",
            "input",
            "label",
            "map",
            "kbd",
            "object",
            "q",
            "ruby",
            "samp",
            "script",
            "select",
            "small",
            "span",
            "strong",
            "sub",
            "sup",
            "textarea",
            "tt",
            "var"
        }.Select(n => XhtmlNs+n));

        public Func<XElement, bool> ExceptInlineElementFromRemovalDelegate { get; set; } = element => 
            element?.Name == XhtmlNs + "span" && (element?.Attribute("class")?.Value.Split().Contains("sentence")??false);

        public bool IsInline(XElement element)
        {
            return element != null && InlineElementNames.Contains(element.Name);
        }

        public bool IsInlineToRemove(XElement element)
        {
            return IsInline(element) && !ExceptInlineElementFromRemovalDelegate(element);
        }

        public IReadOnlyList<XName> HeaderNames { get; }
            = new ReadOnlyCollection<XName>(Enumerable.Range(1, 6).Select(i => XhtmlNs + $"h{i}").ToList());

        private IEnumerable<XElement> ExpandBlockContainers(XElement elem)
        {
            if (elem == null) throw new ArgumentNullException(nameof(elem));
            if (BlockContainerNames?.Contains(elem.Name) ?? false)
            {
                if (elem.Nodes().OfType<XText>().Any(t => !String.IsNullOrWhiteSpace(t.Value)))
                {
                    var lineInfo = (IXmlLineInfo) elem;
                    throw new InvalidOperationException(
                        $"Div element with mixed or pure text content not supported ({lineInfo})");
                }
                return elem.Elements().SelectMany(ExpandBlockContainers);
            }
            return new[] {elem};
        }

        private bool IsXhtmlElement(XElement elem, params string[] names)
        {
            return
                elem != null
                && elem.Name.Namespace == XhtmlNs
                && (names.Contains(elem.Name.LocalName) || (names?.Length??0)==0);

        }

        private IEnumerable<XElement> ExpandTablesAndLists(XElement elem)
        {
            if (elem == null) throw new ArgumentNullException(nameof(elem));
            if (IsXhtmlElement(elem))
            {
                switch (elem.Name.LocalName)
                {
                    case "table":
                        return elem
                            .Elements()
                            .Where(e => IsXhtmlElement(e, "title", "caption", "summary", "thead", "tfoor", "tbody", "tr" ))
                            .SelectMany(ExpandTablesAndLists);
                    case "thead":
                    case "tfoot":
                    case "tbody":
                        return elem.Elements(XhtmlNs + "tr").SelectMany(ExpandTablesAndLists);
                    case "tr":
                        return elem
                            .Elements()
                            .Where(e => IsXhtmlElement(e, "td", "th"))
                            .SelectMany(ExpandTablesAndLists);
                    case "ul":
                    case "ol":
                        return elem
                            .Elements()
                            .Where(e => IsXhtmlElement(e, "title", "caption", "li"))
                            .SelectMany(ExpandTablesAndLists);
                    case "dl":
                        return elem
                            .Elements()
                            .Where(e => IsXhtmlElement(e, "title", "caption", "dt", "dd", "di"))
                            .SelectMany(ExpandTablesAndLists);
                    case "di":
                        return elem
                            .Elements()
                            .Where(e => IsXhtmlElement(e, "dt", "dd"))
                            .SelectMany(ExpandTablesAndLists);
                    case "th":
                    case "td":
                    case "li":
                    case "dd":
                        if (elem.Elements().Any(IsInline) 
                            || elem.Nodes().OfType<XText>().Any(t => !String.IsNullOrWhiteSpace(t.Value)))
                        {
                            if (elem.Elements().Any(e => !IsInline(e)))
                            {
                                var lineInfo = (IXmlLineInfo)elem;
                                throw new InvalidOperationException(
                                    $"{elem.Name.LocalName} with mixed content not supported ({lineInfo})");
                            }
                        }
                        else
                        {
                            return elem.Elements().SelectMany(ExpandTablesAndLists);
                        }
                        break;
                }
            }
            return new[] { elem };

        }

        public IEnumerable<string> AudioFiles => Body
            ?.DescendantNodes()
            .SelectMany(t => t.Annotations<SyncAnnotation>())
            .Select(a => a.Src)
            .Distinct();

        private int audioFileNumber;

        private string AudioFileName => $"aud{audioFileNumber:D5}.{(EncodeMp3 ? "mp3" : "wav")}";

        private string AudioFilePath => Path.Combine(OutputDirectory, AudioFileName);

        private async Task EncodeMp3AudioFile(Stream waveStream, string audioFilePath)
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

        public bool GenerateDaisy202Dtb()
        {
            NormalizeSpaceInXhtmlDocument(XhtmlDocument);
            Utils.SetMeta(XhtmlDocument, "dc:format", "Daisy 2.02");
            MovePageNumbers();
            if (!Synthesize()) return false;
            if (!GenerateDaisy202SmilFiles()) return false;
            int i = 0;
            foreach (var smilFileName in SmilFiles.Keys)
            {
                if (FireProgress(100 * i / SmilFiles.Count,
                    $"Saving smil file {smilFileName} ({i + 1}/{SmilFiles.Count}"))
                {
                    return false;
                }
                SmilFiles[smilFileName].Save(Path.Combine(OutputDirectory, smilFileName));
                i++;
            }
            GenerateNccDocument();
            NccDocument.Save(Path.Combine(OutputDirectory, "ncc.html"));
            XhtmlDocument.Save(XhtmlPath);
            return true;
        }

        public bool Synthesize()
        {
            ValidateSynthesizer();
            audioFileNumber = -1;
            WaveFileWriter writer = null;
            MemoryStream writerStream = null;
            var encodingTaskStack = new Stack<Task>();
            var elements = Body.Elements().SelectMany(ExpandBlockContainers).SelectMany(ExpandTablesAndLists).ToList();
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

        public void RemoveInlineElementReferences()
        {
             foreach (var anno in (Body??new XElement(XhtmlNs+"body"))
                 .DescendantNodes()
                 .SelectMany(n => n.Annotations<SyncAnnotation>()))
            {
                while (anno.Element?.Parent != null && IsInlineToRemove(anno.Element))
                {
                    anno.Element = anno.Element.Parent;
                }
            }
        }

        public bool GenerateDaisy202SmilFiles()
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
            foreach (var kvp in SmilFiles)
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
                (2 + SmilFiles.Count + AudioFiles.Count() + imageFileCount).ToString());
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

        public void MovePageNumbers()
        {
            foreach (var span in XhtmlDocument.Descendants(XhtmlNs + "span").Where(IsPageNumberElement))
            {
                Utils.TrimWhiteSpace(span);
                Utils.AddPageName(span);
                MovePageNumber(span);
            }
        }

        private void MovePageNumber(XElement pageNumSpan)
        {
            var parent = pageNumSpan.Parent;
            if (parent == null || parent == Body || BlockContainerNames.Contains(parent.Name))
            {
                return;
            }
            pageNumSpan.Remove();
            parent.AddAfterSelf(pageNumSpan);
            MovePageNumber(pageNumSpan);
        }

        public void NormalizeSpaceInXhtmlDocument(XDocument doc)
        {
            foreach (var text in doc.DescendantNodes().OfType<XText>())
            {
                text.Value = Regex.Replace(text.Value, @"\s+", " ");
            }
            foreach (var element in doc.Descendants().Where(e => IsPageNumberElement(e) || !IsInline(e)))
            {
                if (element.NextNode is XText nt)
                {
                    nt.Value = $"\n{nt.Value.TrimStart()}";
                }
                else
                {
                    element.AddAfterSelf("\n");
                }
                if (element.PreviousNode is XText pt)
                {
                    pt.Value = $"{pt.Value.TrimEnd()}\n";
                }
                else
                {
                    element.AddBeforeSelf("\n");
                }
            }
        }
    }
}
