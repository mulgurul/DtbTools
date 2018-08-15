using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using NAudio.Wave;

namespace DtbMerger2Library.Daisy202
{
    public static class Utils
    {
        private static readonly List<string> HeadingLocalNames = new List<string> { "h1", "h2", "h3", "h4", "h5", "h6" };

        public static XNamespace XhtmlNs => "http://www.w3.org/1999/xhtml";

        public static XDocument GenerateSkeletonXhtmlDocument()
        {
            return new XDocument(
                new XDeclaration("1.0", "UTF-8", "true"),
                new XDocumentType("html", "-//W3C//DTD XHTML 1.0 Transitional//EN", "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd", null),
                new XElement(
                    Utils.XhtmlNs + "html",
                    new XElement(
                        Utils.XhtmlNs + "head",
                        new XElement(
                            Utils.XhtmlNs + "meta",
                            new XAttribute("http-equiv", "Content-type"),
                            new XAttribute("content", "text/html; charset=utf-8"))),
                    new XElement(Utils.XhtmlNs + "body")));
        }

        public static XDocument GenerateSkeletonSmilDocument()
        {
            return new XDocument(
                new XDeclaration("1.0", "UTF-8", "true"),
                new XDocumentType("smil", "-//W3C//DTD SMIL 1.0//EN", "http://www.w3.org/TR/REC-smil/SMIL10.dtd", null),
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
                        new XElement("seq"))));
        }

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

        public static bool IsHeadingOrParagraph(XElement elem)
        {
            return elem.Name.LocalName == "p" || IsHeading(elem);
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

        public static XElement GetReferencedElement(Uri uri, XDocument destDoc = null)
        {
            if (uri == null) throw new ArgumentNullException(nameof(uri));
            if (destDoc == null)
            {
                destDoc = XDocument.Load(uri.AbsoluteUri, LoadOptions.SetBaseUri);
            }
            var destDocUri = new Uri(destDoc.BaseUri);
            if (!String.Equals(destDocUri.GetLeftPart(UriPartial.Query), uri.GetLeftPart(UriPartial.Query), StringComparison.InvariantCultureIgnoreCase))
            {
                return null;
            }
            if (String.IsNullOrEmpty(uri.Fragment))
            {
                return null;
            }
            return destDoc.XPathSelectElement($"//*[@id='{uri.Fragment.TrimStart('#')}']");
        }

        public static XElement GetReferencedElement(XAttribute uriAttr, XDocument destDoc = null)
        {
            return GetReferencedElement(GetUri(uriAttr), destDoc);

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

        public static XElement CloneWithBaseUri(XElement source) 
        {
            using (var reader = XmlReader.Create(new StringReader(source.ToString()), new XmlReaderSettings(),
                source.BaseUri))
            {
                if (reader.Read())
                {
                    return XElement.Load(reader, LoadOptions.SetBaseUri);
                }

                return null;
            }
        }

        public static bool IsReferenceTo(Uri source, Uri dest)
        {
            if (source == null || dest == null)
            {
                return false;
            }

            if (source.IsAbsoluteUri != dest.IsAbsoluteUri)
            {
                return false;
            }

            if (!source.IsAbsoluteUri)
            {
                source = new Uri(new Uri("http://temp.org"), source);
                dest = new Uri(new Uri("http://temp.org"), dest);
            }

            return dest.GetLeftPart(UriPartial.Query).ToLowerInvariant() == source.GetLeftPart(UriPartial.Query).ToLowerInvariant();
        }

        public static TimeSpan GetAudioFileDuration(Uri audioFile)
        {
            var path = Uri.UnescapeDataString(audioFile.LocalPath);
            if (!File.Exists(path))
            {
                throw new InvalidOperationException($"Could not find audio file {audioFile}");
            }

            return new AudioFileReader(path).TotalTime;
        }

        public static XElement CreateOrGetMeta(XDocument doc, string name)
        {
            var head = doc.Root?.Element(doc.Root.Name.Namespace + "head");
            if (head == null)
            {
                return null;
            }

            var meta = head.Elements(head.Name.Namespace + "meta").FirstOrDefault(m => m.Attribute("name")?.Value == name);
            if (meta == null)
            {
                meta = new XElement(head.Name.Namespace + "meta", new XAttribute("name", name));
                head.Add(meta);
            }

            return meta;
        }

        public static string GetHHMMSSFromTimeSpan(TimeSpan val)
        {
            return TimeSpan.FromSeconds(Math.Round(val.TotalSeconds)).ToString(@"hh\:mm\:ss");
        }

        public static bool IsNccUri(Uri uri)
        {
            if (uri == null) throw new ArgumentNullException(nameof(uri));
            return new[] {"ncc.htm", "ncc.html"}.Contains(uri.GetLeftPart(UriPartial.Query).Split('/').Last());
        }

        /// <summary>
        /// Tries to get a ncc heading <see cref="Uri"/> from a textual content document heading <see cref="Uri"/> (that corresponds)
        /// </summary>
        /// <param name="contentHeadingUri">The textual content document heading <see cref="Uri"/></param>
        /// <returns>The ncc heading <see cref="Uri"/> or <c>null</c> if a such could not be found</returns>
        public static Uri GetNccHeadingUriFromContentHeadingUri(Uri contentHeadingUri)
        {
            var smilUri = GetReferencedElement(contentHeadingUri)
                ?.Descendants(XhtmlNs + "a")
                .Select(a => a.Attribute("href"))
                .Select(GetUri)
                .FirstOrDefault();
            if (smilUri == null)
            {
                return null;
            }
            var smilPar = GetReferencedElement(smilUri);
            if (smilPar == null)
            {
                return null;
            }

            if (smilPar.Name == "text")
            {
                smilPar = smilPar.Parent;
            }

            if (smilPar?.Name != "par")
            {
                return null;
            }
            var ncc = new[] {"ncc.htm", "ncc.html"}
                .Select(fn => new Uri(new Uri(smilPar.BaseUri??""), fn))
                .Where(uri => File.Exists(uri.LocalPath))
                .Select(nccUri => XDocument.Load(nccUri.AbsoluteUri, LoadOptions.SetBaseUri))
                .FirstOrDefault();

            return ncc
                ?.Root
                ?.Element(XhtmlNs + "body")
                ?.Elements()
                .Where(IsHeading)
                .Where(h =>
                    h
                        .Descendants(XhtmlNs + "a")
                        .SelectMany(a => a.Attributes("href"))
                        .Select(GetUri)
                        .Select(uri => GetReferencedElement(uri, smilPar.Document))
                        .Any(elem => elem == smilPar || smilPar.Elements("text").Any(text => text == elem))
                    && h.Attribute("id") != null)
                .Select(h => new Uri(new Uri(h.BaseUri), $"#{h.Attribute("id")?.Value??""}"))
                .FirstOrDefault();
        }
    }
}
