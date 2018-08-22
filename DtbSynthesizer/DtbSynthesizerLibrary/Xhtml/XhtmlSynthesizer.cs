using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using NAudio.Codecs;
using NAudio.Wave;

namespace DtbSynthesizerLibrary.Xhtml
{
    public class XhtmlSynthesizer
    {
        public static XNamespace XhtmlNs => Utils.XhtmlNs;

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

        private readonly IDictionary<string, XDocument> smilFilesByFileName = new Dictionary<string, XDocument>();

        public IReadOnlyDictionary<string, XDocument> SmilFiles =>
            new ReadOnlyDictionary<string, XDocument>(smilFilesByFileName);

        public XDocument NccDocument;

        public Func<CultureInfo, IXmlSynthesizer> SynthesizerSelector { get; set; } 
            = Utils.GetPrefferedXmlSynthesizerForCulture;

        public IXmlSynthesizer DefaultSynthesizer { get; set; }

        public WaveFormat AudioWaveFormat { get; set; } = new WaveFormat(44100, 1);

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

        public bool IsInlineToRemove(XElement element)
        {
            return
                element != null
                && InlineElementNames.Contains(element.Name)
                && !ExceptInlineElementFromRemovalDelegate(element);
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

        public IEnumerable<string> AudioFiles => Body
            ?.DescendantNodes()
            .OfType<XText>()
            .SelectMany(t => t.Annotations<SyncAnnotation>())
            .Select(a => a.Src)
            .Distinct();

        private int waveFileNumber;

        private string WaveFileName => $"AUD{waveFileNumber:D5}.wav";

        private string WaveFilePath => Path.Combine(
            Path.GetDirectoryName(XhtmlPath)??Directory.GetCurrentDirectory(),
            WaveFileName);


        public bool Synthesize()
        {
            ValidateSynthesizer();
            waveFileNumber = -1;
            var dur = TimeSpan.Zero;
            WaveFileWriter writer = null;
            var elements = Body.Elements().SelectMany(ExpandBlockContainers).ToList();
            for (int i = 0; i < elements.Count; i++)
            {
                if (FireProgress(
                    100 * i / elements.Count,
                    $"Synthesizing element {i + 1} of {elements.Count} to {WaveFileName}"))
                {
                    return false;
                }
                var elem = elements[i];
                if (HeaderNames.Contains(elem.Name) || writer == null)
                {
                    writer?.Close();
                    waveFileNumber++;
                    writer = new WaveFileWriter(WaveFilePath, AudioWaveFormat);
                }
                var ci = Utils.SelectCulture(elem);
                var synth = CultureInfo.InvariantCulture.Equals(ci)
                    ? DefaultSynthesizer
                    : SynthesizerSelector(ci);
                dur += synth.SynthesizeElement(elem, writer, WaveFileName);
            }
            writer?.Close();
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

        public void GenerateDaisy202SmilFiles()
        {
            var xhtmlFileName = Path.GetFileName(XhtmlPath);
            var smilFiles = new Dictionary<string, XDocument>();
            foreach (var anno in (Body ?? new XElement(XhtmlNs + "body"))
                .DescendantNodes()
                .SelectMany(n => n.Annotations<SyncAnnotation>())
                .Where(anno => 
                    (anno.Src?.EndsWith(".wav")??false) 
                    && !String.IsNullOrEmpty(anno.Element?.Attribute("id")?.Value)))
            {
                var smilName = $"{anno.Src.Substring(0, anno.Src.Length - 4)}.smil".ToLowerInvariant();
                if (!smilFiles.ContainsKey(smilName))
                {
                    smilFiles.Add(smilName, Utils.GenerateSkeletonDaisy202SmilDocument(new Uri(new Uri(XhtmlDocument.BaseUri), smilName).AbsoluteUri));
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
                if (textReference.Equals(lastTextReference, StringComparison.InvariantCulture))
                {
                    mainSeq.Elements("par").Last().Element("seq")?.Add(audio);
                }
                else
                {
                    mainSeq.Add(new XElement(
                        "par",
                        new XAttribute("id", $"par_{mainSeq.Elements("par").Count():D5}"),
                        new XElement("text", new XAttribute("src", textReference))),
                        new XElement("seq", audio));
                }
            }

            var totalElapsedTime = TimeSpan.Zero;
            foreach (var kvp in smilFiles.OrderBy(o => o.Key))
            {
                var audioFile = $"{kvp.Key.Substring(0, kvp.Key.Length-5)}.wav";
                var timeInThisSmil = TimeSpan.FromMilliseconds(
                    Body
                        ?.DescendantNodes()
                        .SelectMany(n => n.Annotations<SyncAnnotation>())
                        .Where(anno => audioFile.Equals(anno.Src, StringComparison.InvariantCultureIgnoreCase))
                        .Sum(anno => anno.ClipEnd.Subtract(anno.ClipBegin).TotalMilliseconds)
                    ??0.0);
                Utils.CreateOrGetMeta(kvp.Value, "ncc:totalElapsedTime")
                    .SetAttributeValue("value", Utils.GetHHMMSSFromTimeSpan(totalElapsedTime));
                Utils.CreateOrGetMeta(kvp.Value, "ncc:timeInThisSmil")
                    .SetAttributeValue("value", Utils.GetHHMMSSFromTimeSpan(timeInThisSmil));
                Utils.CreateOrGetMeta(kvp.Value, "ncc:generator")
                    .SetAttributeValue("value", Utils.Generator);

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
        }

        public void GenerateNccDocument()
        {
            var ncc = Utils.GenerateSkeletonXhtmlDocument(new Uri(new Uri(XhtmlDocument.BaseUri), "ncc.html").AbsoluteUri);
            ncc.Root?.Element(XhtmlNs + "head")?.Add(XhtmlDocument.Root?.Element(XhtmlNs + "head")?.Elements());
            var nsMgr = new XmlNamespaceManager(ncc.CreateReader().NameTable??new NameTable());
            nsMgr.AddNamespace("x", XhtmlNs.NamespaceName);
            var body = ncc.XPathSelectElements("/x:html/x:body", nsMgr).Single();
            foreach (var kvp in SmilFiles)
            {
                var firstSrcId = kvp
                    .Value
                    .Descendants("text")
                    .Select(text => text.Attribute("src")?.Value)
                    .First(src => !String.IsNullOrEmpty(src)).Split('#').Last();
                var heading = new XElement(Body.Descendants().First(e => e.Attribute("id")?.Value == firstSrcId));
                var headingNodes = heading.Nodes().ToList();
                var a = new XElement(
                    XhtmlNs+"a",
                    new XAttribute("href", $"{kvp.Key}#par_00000"));
                foreach (var e in headingNodes)
                {
                    e.Remove();
                    a.Add(e);
                }
                heading.Add(a);
                body.Add(heading);
            }
            Utils.CreateOrGetMeta(ncc, "dc:format").SetAttributeValue("value", "Daisy 2.02");
            Utils.CreateOrGetMeta(ncc, "ncc:files").SetAttributeValue("value", 2+SmilFiles.Count+AudioFiles.Count());
            Utils.CreateOrGetMeta(ncc, "ncc:totalTime").SetAttributeValue(
                "value",
                Utils.GetHHMMSSFromTimeSpan(
                    TimeSpan.FromSeconds(
                        Body.DescendantNodes().SelectMany(n =>
                            n.Annotations<SyncAnnotation>().Select(anno => (anno.ClipEnd - anno.ClipBegin).TotalSeconds)).Sum())));
            Utils.CreateOrGetMeta(ncc, "ncc:tocItems").SetAttributeValue("value", body.Elements().Count());

            Utils.CreateOrGetMeta(ncc, "ncc:multimediaType").SetAttributeValue("value", "audioFullText");
            Utils.CreateOrGetMeta(ncc, "ncc:generator").SetAttributeValue("value", Utils.Generator);


            NccDocument = ncc;
        }


    }
}
