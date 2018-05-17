using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;

namespace DtbMerger2Library.Daisy202
{
    public static class Utils
    {
        private static readonly List<string> HeadingLocalNames = new List<string> { "h1", "h2", "h3", "h4", "h5", "h6" };

        public static XNamespace XhtmlNs => "http://www.w3.org/1999/xhtml";

        public static XName GetSubHeadingName(XName headingName)
        {
            var index = HeadingLocalNames.IndexOf(headingName.LocalName);
            if (0 <= index && index + 1 < HeadingLocalNames.Count)
            {
                return headingName.Namespace + HeadingLocalNames[index + 1];
            }

            return null;
        }

        public static bool IsHeading(XElement elem)
        {
            return HeadingLocalNames.Contains(elem?.Name?.LocalName);
        }

        public static Uri GetUri(XAttribute uriAttr)
        {
            if (uriAttr == null)
            {
                return null;
            }

            if (String.IsNullOrEmpty(uriAttr.BaseUri))
            {
                return new Uri(uriAttr.Value);
            }
            return new Uri(new Uri(uriAttr.BaseUri), uriAttr.Value);
        }

        public static XElement GetReferencedElement(XDocument destDoc, Uri uri)
        {
            if (destDoc == null) throw new ArgumentNullException(nameof(destDoc));
            if (uri == null) throw new ArgumentNullException(nameof(uri));
            var destDocUri = new Uri(destDoc.BaseUri);
            if (!String.Equals(destDocUri.AbsolutePath, uri.AbsolutePath, StringComparison.InvariantCultureIgnoreCase))
            {
                return null;
            }
            if (String.IsNullOrEmpty(uri.Fragment))
            {
                return null;
            }
            return destDoc.XPathSelectElement($"//*[@id='{uri.Fragment.TrimStart('#')}']");
        }

        public static XElement GetReferencedElement(XDocument destDoc, XAttribute uriAttr)
        {
            return GetReferencedElement(destDoc, GetUri(uriAttr));

        }

        public static TimeSpan ParseSmilClip(string val)
        {
            if (val == null) throw new ArgumentNullException(nameof(val));
            if (String.IsNullOrWhiteSpace(val))
            {
                throw new ArgumentException($"Value is empty", nameof(val));
            }
            val = val.Trim();
            if (val.StartsWith("npt=") && val.EndsWith("s"))
            {
                var secs = Double.Parse(
                    val.Substring(4, val.Length - 5), 
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture);
                return TimeSpan.FromSeconds(secs);
            }
            else
            {
                throw new ArgumentException($"Value {val} is not a valid Daisy 2.02 smil clip value", nameof(val));
            }
        }
    }
}
