using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace DtbSynthesizerLibrary
{
    public static class Utils
    {
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
                return lang==null ? CultureInfo.InvariantCulture : new CultureInfo(lang);

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
    }
}
