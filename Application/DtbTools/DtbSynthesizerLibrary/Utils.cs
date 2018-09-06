using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Xsl;
using DtbSynthesizerLibrary.Xml;
using Saxon.Api;

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
            if (CultureInfo.InvariantCulture.Equals(ci))
            {
                return synthesizerList.FirstOrDefault();
            }
            return 
                synthesizerList.FirstOrDefault(s => s.VoiceInfo.Culture.Equals(ci))
                ?? synthesizerList.FirstOrDefault(s =>
                    s.VoiceInfo.Culture.TwoLetterISOLanguageName == ci.TwoLetterISOLanguageName)
                ?? synthesizerList.FirstOrDefault();
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
                    new XDocumentType(
                        "html", 
                        "-//W3C//DTD XHTML 1.0 Transitional//EN", 
                        "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd", 
                        null),
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
                    new XDocumentType(
                        "smil",
                        "-//W3C//DTD SMIL 1.0//EN",
                        "http://www.w3.org/TR/REC-SMIL/SMIL10.dtd",
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
            var res = XDocument.Load(
                (baseUri == null)
                    ? document.CreateReader()
                    : XmlReader.Create(new StringReader(document.ToString()),
                        new XmlReaderSettings() {DtdProcessing = DtdProcessing.Parse}, baseUri),
                LoadOptions.SetBaseUri);
            if (res.DocumentType != null && String.IsNullOrWhiteSpace(res.DocumentType.InternalSubset))
            {
                res.DocumentType.InternalSubset = null;
            }
            return res;
        }

        /// <summary>
        /// Gets or creates a meta <see cref="XElement"/> in a <see cref="XDocument"/> with a given name.
        /// </summary>
        /// <param name="doc">The <see cref="XDocument"/></param>
        /// <param name="name">The name of the meta <see cref="XElement"/> (that is the value of the name <see cref="XAttribute"/></param>
        /// <param name="createIfMissing">Creates and adds the meta if missing</param>
        /// <returns>
        /// An existing or, if not already present, newly added meta <see cref="XElement"/> from the head section of <paramref name="doc"/>
        /// </returns>
        private static XElement GetMeta(XDocument doc, string name, bool createIfMissing = true)
        {
            var head = doc.Root?.Element(doc.Root.Name.Namespace + "head");
            if (head == null)
            {
                return null;
            }

            var meta = head.Elements(head.Name.Namespace + "meta")
                .FirstOrDefault(m => m.Attribute("name")?.Value == name);
            if (meta == null && createIfMissing)
            {
                meta = new XElement(head.Name.Namespace + "meta", new XAttribute("name", name));
                head.Add(meta);
            }

            return meta;
        }

        public static string GetMetaContent(XDocument doc, string name)
        {
            return GetMeta(doc, name, false)?.Attribute("content")?.Value;
        }

        public static XElement SetMeta(XDocument doc, string name, string content)
        {
            var meta = GetMeta(doc, name);
            meta?.SetAttributeValue("content", content);
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

        private static readonly Processor Processor = new Processor();
        private static readonly XsltCompiler XsltCompiler = Processor.NewXsltCompiler();

        private static IDictionary<string, XsltExecutable> embeddedXsltExecutables;

        public static IDictionary<string, XsltExecutable> EmbeddedXsltExecutables
        {
            get
            {
                if (embeddedXsltExecutables == null)
                {
                    embeddedXsltExecutables = new Dictionary<string, XsltExecutable>();
                    //Write the embedded xslts to a temp dir
                    var temp = Path.GetTempFileName();
                    File.Delete(temp);
                    Directory.CreateDirectory(temp);
                    try
                    {
                        foreach (var xsltName in Assembly
                            .GetExecutingAssembly()
                            .GetManifestResourceNames()
                            .Where(n => n.StartsWith($"{typeof(Utils).Namespace}.Xslt.") && n.EndsWith(".xsl")))
                        {
                            using (var resStr = Assembly.GetExecutingAssembly().GetManifestResourceStream(xsltName))
                            {
                                using (var fs = new FileStream(
                                    Path.Combine(temp, xsltName.Substring((typeof(Utils).Namespace??"").Length + 6)),
                                    FileMode.CreateNew,
                                    FileAccess.Write))
                                {
                                    resStr?.CopyTo(fs);
                                }
                            }
                        }
                        foreach (var xsltFile in Directory.GetFiles(temp).Where(n => !Path.Combine(temp, "l10n.xsl").Equals(n)))
                        {
                            embeddedXsltExecutables.Add(
                                Path.GetFileName(xsltFile),
                                XsltCompiler.Compile(new Uri(xsltFile)));
                        }
                    }
                    finally
                    {
                        if (Directory.Exists(temp))
                        {
                            Directory.Delete(temp, true);
                        }
                    }

                }
                return new ReadOnlyDictionary<string, XsltExecutable>(embeddedXsltExecutables);
            }
        }

        private static IDictionary<string, XsltExecutable> dtbookToXhtmlTransformsByVersion;

        public static IDictionary<string, XsltExecutable> DtbookToXhtmlTransformsByVersion =>
            dtbookToXhtmlTransformsByVersion ?? (dtbookToXhtmlTransformsByVersion =
                new ReadOnlyDictionary<string, XsltExecutable>(
                    new Dictionary<string, XsltExecutable>()
                    {
                        {"1.1.0", EmbeddedXsltExecutables["dtbook110to2005-1.xsl"]},
                        {"2005-1", EmbeddedXsltExecutables["dtbook2005-1to2.xsl"]},
                        {"2005-2", EmbeddedXsltExecutables["dtbook2005-2to3.xsl"]},
                        {"2005-3", EmbeddedXsltExecutables["dtbook2xhtml.xsl"]}
                    }));

        public static XDocument TransformDtbookToXhtml(XDocument dtbook)
        {
            if (dtbook == null) throw new ArgumentNullException(nameof(dtbook));
            if (dtbook.Root?.Name.LocalName != "dtbook")
            {
                throw new InvalidOperationException("Input document is not a dtbook document");
            }
            var version = dtbook.Root.Attribute("version")?.Value ?? "";
            if (!DtbookToXhtmlTransformsByVersion.ContainsKey(version))
            {
                throw new InvalidOperationException($"dtbook version {version} not supported");
            }
            var input = Processor.NewDocumentBuilder().Build(dtbook.CreateReader());
            var strWr = new StringWriter();
            var serializer = Processor.NewSerializer(strWr);
            var trans = DtbookToXhtmlTransformsByVersion[version].Load();
            trans.InitialContextNode = input;
            trans.Run(serializer);
            var res = XDocument.Parse(strWr.ToString());
            if (res.Root?.Name.LocalName == "dtbook")
            {
                return TransformDtbookToXhtml(res);
            }
            return res;
        }

        public static TimeSpan AdjustClipTime(TimeSpan clipTime, TimeSpan startOffset, double factor)
        {
            return startOffset + TimeSpan.FromSeconds((clipTime - startOffset).TotalSeconds * factor);
        }

    }
}
