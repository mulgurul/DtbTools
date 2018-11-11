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
using DtbSynthesizerLibrary.Xml;
using NAudio.Lame;
using NAudio.Wave;

namespace DtbSynthesizerLibrary.Xhtml
{
    public class XhtmlSynthesizer
    {
        private int audioFileNumber;
        public static XNamespace XhtmlNs => Utils.XhtmlNs;
        public XDocument XhtmlDocument { get; set; }

        protected Func<XElement, string> TextToSynthesizeDelegate { get; set; } = null;

        public bool NewAudioFileAtHeading { get; set; } = true;

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

        public Func<CultureInfo, IXmlSynthesizer> SynthesizerSelector { get; set; } 
            = (ci) => Utils.GetPrefferedXmlSynthesizerForCulture(ci);

        public IXmlSynthesizer DefaultSynthesizer { get; set; }
        public WaveFormat AudioWaveFormat { get; set; } = new WaveFormat(22050, 1);

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

        public IReadOnlyList<XName> HeaderNames { get; }
            = new ReadOnlyCollection<XName>(Enumerable.Range(1, 6).Select(i => XhtmlNs + $"h{i}").ToList());

        public IEnumerable<string> AudioFiles => Body
            ?.DescendantNodes()
            .SelectMany(t => t.Annotations<SyncAnnotation>())
            .Select(a => a.Src)
            .Distinct();

        protected string AudioFileName => $"aud{audioFileNumber:D5}.{(EncodeMp3 ? "mp3" : "wav")}";
        private string AudioFilePath => Path.Combine(OutputDirectory, AudioFileName);

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

        public bool IsInline(XElement element)
        {
            return element != null && InlineElementNames.Contains(element.Name);
        }

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
                            .Where(e => IsXhtmlElement(e, "title", "caption", "summary", "thead", "tfoot", "tbody", "tr" ))
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
                if ((HeaderNames.Contains(elem.Name) && NewAudioFileAtHeading) || writer == null)
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
                if (TextToSynthesizeDelegate != null)
                {
                    synth.TextToSynthesizeDelegate = TextToSynthesizeDelegate;
                }
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