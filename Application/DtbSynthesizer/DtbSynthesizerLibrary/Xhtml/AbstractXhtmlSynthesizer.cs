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
    public abstract class AbstractXhtmlSynthesizer : AbstractSynthesizer
    {
        public static XNamespace XhtmlNs => Utils.XhtmlNs;
        public XDocument XhtmlDocument { get; set; }

        public XElement Body => XhtmlDocument?.Root?.Element(XhtmlNs + "body");

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

        protected void ValidateSynthesizer()
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
            var languages = ElementsToSynthesize.Select(Utils.GetLanguage).Distinct().Where(lang => lang != null).ToList();
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
                ElementsToSynthesize.Select(Utils.SelectCulture).Any(CultureInfo.InvariantCulture.Equals) 
                && DefaultSynthesizer == null)
            {
                throw new InvalidOperationException($"{nameof(DefaultSynthesizer)} is null");
            }
        }

        private bool IsInline(XElement element)
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

        public IReadOnlyList<XElement> ElementsToSynthesize =>
            Body.Elements().SelectMany(ExpandBlockContainers).SelectMany(ExpandTablesAndLists).ToList().AsReadOnly();

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