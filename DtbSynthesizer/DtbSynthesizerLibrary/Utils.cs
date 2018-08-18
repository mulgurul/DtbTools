using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace DtbSynthesizerLibrary
{
    public static class Utils
    {
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
    }
}
