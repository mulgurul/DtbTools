using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace DtbSynthesizerLibrary
{
    public static class Utils
    {
        public static XNamespace XhtmlNs => "http://www.w3.org/1999/xhtml";

        /// <summary>
        /// Gets the language of an <see cref="XElement"/>
        /// from the xml:lang or lang <see cref="XAttribute"/>s.
        /// If both attributes have non-whitespace value, xml:lang takes precedent
        /// </summary>
        /// <param name="elem">The <see cref="XElement"/></param>
        /// <returns>The language or <c>null</c> is not present</returns>
        public static string GetLanguage(XElement elem)
        {
            if (elem == null)
            {
                return null;
            }
            var lang = elem.Attribute(XNamespace.Xml + "lang")?.Value;
            if (String.IsNullOrWhiteSpace(lang))
            {
                lang = elem.Attribute("lang")?.Value;
            }
            return String.IsNullOrWhiteSpace(lang) ? null : lang;
        }

        public static CultureInfo SelectCulture(XNode node)
        {
            var lang =
                GetLanguage(node as XElement)
                ?? GetLanguage(node
                    .Ancestors()
                    .FirstOrDefault(elem => GetLanguage(elem) != null));
            try
            {
                return lang == null ? CultureInfo.InvariantCulture : new CultureInfo(lang);

            }
            catch (CultureNotFoundException)
            {
                return CultureInfo.InvariantCulture;
            }
        }

        public static IEnumerable<IXmlSynthesizer> GetAllSynthesizers()
        {
            return SystemSpeechXmlSynthesizer
                .Synthesizers
                .Concat(MicrosoftSpeechXmlSynthesizer.Synthesizers);
        }

        public static IXmlSynthesizer GetPrefferedXmlSynthesizerForCulture(CultureInfo ci)
        {
            return GetPrefferedXmlSynthesizerForCulture(ci, GetAllSynthesizers().ToList());
        }

        public static IXmlSynthesizer GetPrefferedXmlSynthesizerForCulture(
            CultureInfo ci,
            IReadOnlyCollection<IXmlSynthesizer> synthesizerList)
        {
            if (!ci.IsNeutralCulture)
            {
                return
                    synthesizerList.FirstOrDefault(s => s.VoiceInfo.Culture.Equals(ci))
                    ?? synthesizerList.FirstOrDefault(s =>
                        s.VoiceInfo.Culture.TwoLetterISOLanguageName == ci.TwoLetterISOLanguageName)
                    ?? synthesizerList.FirstOrDefault();
            }
            return synthesizerList.FirstOrDefault();

        }

        private static readonly Regex GeneratedIdRegex = new Regex("^IX\\d{5,}$");

        public static int SetMissingIds(XElement elem)
        {
            return elem
                .DescendantsAndSelf()
                .Where(e => String.IsNullOrEmpty(e.Attribute("id")?.Value))
                .Select(e =>
                {
                    e.SetAttributeValue("id", GenerateNewId(e.Document));
                    return 1;
                })
                .Sum();
        }

        public static string GenerateNewId(XDocument doc)
        {
            var ids = new HashSet<ulong>(doc
                .Descendants()
                .Select(elem => elem.Attribute("id")?.Value ?? "")
                .Distinct()
                .Where(id => GeneratedIdRegex.IsMatch(id))
                .Select(id => UInt64.Parse(id.Substring(2))));
            ulong nextId = 0;
            while (ids.Contains(nextId))
            {
                nextId++;
            }
            return $"IX{nextId:D5}";
        }

        /// <summary>
        /// Generates a skeleton xhtml <see cref="XDocument"/>
        /// </summary>
        /// <returns>The skeleton xhtml <see cref="XDocument"/></returns>
        public static XDocument GenerateSkeletonXhtmlDocument(string baseUri = null)
        {
            return CloneWithBaseUri(
                new XDocument(
                    new XDeclaration("1.0", "UTF-8", "true"),
                    new XDocumentType("html", "-//W3C//DTD XHTML 1.0 Transitional//EN",
                        "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd", null),
                    new XElement(
                        Utils.XhtmlNs + "html",
                        new XElement(
                            Utils.XhtmlNs + "head",
                            new XElement(
                                Utils.XhtmlNs + "meta",
                                new XAttribute("http-equiv", "Content-type"),
                                new XAttribute("content", "text/html; charset=utf-8"))),
                        new XElement(Utils.XhtmlNs + "body"))),
                baseUri);
        }

        /// <summary>
        /// Generates a skeleton SMIL 1.0 <see cref="XDocument"/> for a Daisy 2.02 DTB
        /// </summary>
        /// <returns>The skeleton SMIL 1.0 <see cref="XDocument"/></returns>
        public static XDocument GenerateSkeletonDaisy202SmilDocument(string baseUri = null)
        {
            return CloneWithBaseUri(
                new XDocument(
                    new XDeclaration("1.0", "UTF-8", "true"),
                    new XDocumentType("smil", "-//W3C//DTD SMIL 1.0//EN", "http://www.w3.org/TR/REC-smil/SMIL10.dtd",
                        null),
                    new XElement(
                        "smil",
                        new XElement(
                            "head",
                            new XElement(
                                "meta",
                                new XAttribute("name", "dc:format"),
                                new XAttribute("content", "Daisy 2.02")),
                            new XElement(
                                "layout",
                                new XElement("region", new XAttribute("id", "txtView")))),
                        new XElement(
                            "body",
                            new XElement("seq")))),
                baseUri);
        }

        public static XElement CloneWithBaseUri(XElement element, string baseUri = null)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            baseUri = baseUri ?? element.BaseUri;
            return XElement.Load(
                (baseUri == null)
                    ? element.CreateReader()
                    : XmlReader.Create(new StringReader(element.ToString()), new XmlReaderSettings(), baseUri),
                LoadOptions.SetBaseUri);
        }

        public static XDocument CloneWithBaseUri(XDocument document, string baseUri = null)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            baseUri = baseUri ?? document.BaseUri;
            return XDocument.Load(
                (baseUri == null)
                    ? document.CreateReader()
                    : XmlReader.Create(new StringReader(document.ToString()),
                        new XmlReaderSettings() {DtdProcessing = DtdProcessing.Ignore}, baseUri),
                LoadOptions.SetBaseUri);
        }

        /// <summary>
        /// Gets or creates a meta <see cref="XElement"/> in a <see cref="XDocument"/> with a given name.
        /// </summary>
        /// <param name="doc">The <see cref="XDocument"/></param>
        /// <param name="name">The name of the meta <see cref="XElement"/> (that is the value of the name <see cref="XAttribute"/></param>
        /// <returns>
        /// An existing or, if not already present, newly added meta <see cref="XElement"/> from the head section of <paramref name="doc"/>
        /// </returns>
        public static XElement CreateOrGetMeta(XDocument doc, string name)
        {
            var head = doc.Root?.Element(doc.Root.Name.Namespace + "head");
            if (head == null)
            {
                return null;
            }

            var meta = head.Elements(head.Name.Namespace + "meta")
                .FirstOrDefault(m => m.Attribute("name")?.Value == name);
            if (meta == null)
            {
                meta = new XElement(head.Name.Namespace + "meta", new XAttribute("name", name));
                head.Add(meta);
            }

            return meta;
        }

        /// <summary>
        /// Gets a hh:mm:ss value of a <see cref="TimeSpan"/> with rounded seconds
        /// </summary>
        /// <param name="val">The <see cref="TimeSpan"/></param>
        /// <returns>The hh:mm:ss value</returns>
        public static string GetHHMMSSFromTimeSpan(TimeSpan val)
        {
            return TimeSpan.FromSeconds(Math.Round(val.TotalSeconds)).ToString(@"hh\:mm\:ss");
        }

        public static string Generator =>
            $"{Assembly.GetExecutingAssembly().GetName().Name} v{Assembly.GetExecutingAssembly().GetName().Version}";
    }
}
