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
    /// <summary>
    /// Static utility class, mainly focused on Daisy 2.02 
    /// </summary>
    public static class Utils
    {
        private static readonly List<string> HeadingLocalNames = new List<string> { "h1", "h2", "h3", "h4", "h5", "h6" };

        /// <summary>
        /// The xhtml <see cref="XNamespace"/>
        /// </summary>
        public static XNamespace XhtmlNs => "http://www.w3.org/1999/xhtml";

        /// <summary>
        /// Generates a skeleton xhtml <see cref="XDocument"/>
        /// </summary>
        /// <returns>The skeleton xhtml <see cref="XDocument"/></returns>
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

        /// <summary>
        /// Generates a skeleton SMIL 1.0 <see cref="XDocument"/>
        /// </summary>
        /// <returns>The skeleton SMIL 1.0 <see cref="XDocument"/></returns>
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

        /// <summary>
        /// Returns the <see cref="XName"/> of an xhtml sub-heading, given an xhtml heading name
        /// </summary>
        /// <param name="headingName">The xhtml heading <see cref="XName"/></param>
        /// <returns>
        /// The xhtml sub-heading <see cref="XName"/> 
        /// h2 if heading is h1, h3 if heading is h2 ect. 
        /// <c>null</c> if <paramref name="headingName"/> is not an xhtml heading with a sub-heading (that is h1-h5)
        /// </returns>
        public static XName GetSubHeadingName(XName headingName)
        {
            var index = HeadingLocalNames.IndexOf(headingName.LocalName);
            if (0 <= index && index + 1 < HeadingLocalNames.Count)
            {
                return headingName.Namespace + HeadingLocalNames[index + 1];
            }

            return null;
        }

        /// <summary>
        /// Determines if an <see cref="XElement"/> is a xhtml heading (h1-h6)
        /// </summary>
        /// <param name="elem">The <see cref="XElement"/></param>
        /// <returns>A <see cref="bool"/> indicating if <paramref name="elem"/> is a heading</returns>
        public static bool IsHeading(XElement elem)
        {
            return HeadingLocalNames.Contains(elem?.Name?.LocalName);
        }

        /// <summary>
        /// Determined if an element is a heading or a paragraph (h1-6 or p)
        /// </summary>
        /// <param name="elem">The <see cref="XElement"/></param>
        /// <returns>A <see cref="bool"/> indicating if <paramref name="elem"/> is a heading or a paragraph</returns>
        public static bool IsHeadingOrParagraph(XElement elem)
        {
            return elem.Name.LocalName == "p" || IsHeading(elem);
        }

        /// <summary>
        /// Gets the <see cref="Uri"/> represented by a <see cref="XAttribute"/>
        /// </summary>
        /// <param name="uriAttr">The <see cref="XAttribute"/></param>
        /// <returns>
        /// The <see cref="Uri"/> represented by <paramref name="uriAttr"/>.T
        /// The <see cref="XObject.BaseUri"/> of is used as base of the returned <see cref="Uri"/>, if present.
        /// If <paramref name="uriAttr"/> is <c>null</c>, <c>null</c> is returned
        /// </returns>
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

        /// <summary>
        /// Gets the element referenced by an <see cref="Uri"/>
        /// </summary>
        /// <param name="uri">The <see cref="Uri"/></param>
        /// <param name="destDoc">
        /// The destination <see cref="XDocument"/>. 
        /// If <c>null</c>, the destination <see cref="XDocument"/> is loaded from <paramref name="uri"/>.
        /// </param>
        /// <returns>
        /// The referenced <see cref="XElement"/>.
        /// <c>null</c> if <paramref name="uri"/> does not point to <paramref name="destDoc"/>,
        /// if <paramref name="uri"/> has no <see cref="Uri.Fragment"/>
        /// or if no <see cref="XElement"/> in <paramref name="destDoc"/> exists that matched the <see cref="Uri.Fragment"/> of <paramref name="uri"/>
        /// </returns>
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

        /// <summary>
        /// Gets the element referenced by an <see cref="Uri"/> as represented by an <see cref="XAttribute"/>
        /// </summary>
        /// <param name="uriAttr">The <see cref="XAttribute"/> representing the <see cref="Uri"/></param>
        /// <param name="destDoc">
        /// The destination <see cref="XDocument"/>. 
        /// If <c>null</c>, the destination <see cref="XDocument"/> is loaded from the <see cref="Uri"/>.
        /// </param>
        /// <returns>
        /// The referenced <see cref="XElement"/>.
        /// <c>null</c> if <paramref name="uriAttr"/> is not an <see cref="Uri"/> (as returned by <see cref="GetUri"/>)
        /// if <paramref name="uriAttr"/> does not point to <paramref name="destDoc"/>,
        /// if <paramref name="uriAttr"/> has no <see cref="Uri.Fragment"/>
        /// or if no <see cref="XElement"/> in <paramref name="destDoc"/> exists that matched the <see cref="Uri.Fragment"/> of <paramref name="uriAttr"/>
        /// </returns>
        public static XElement GetReferencedElement(XAttribute uriAttr, XDocument destDoc = null)
        {
            var uri = GetUri(uriAttr);
            if (uri == null) return null;
            return GetReferencedElement(uri, destDoc);

        }

        /// <summary>
        /// Parses a Daisy 2.02 SMIL 1.0 file clip attribute value
        /// </summary>
        /// <param name="val">The value</param>
        /// <returns>The <see cref="TimeSpan"/> equivalent of the value</returns>
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

        /// <summary>
        /// Clones an <see cref="XElement"/> with <see cref="XObject.BaseUri"/>
        /// </summary>
        /// <param name="source">The uri1 <see cref="XDocument"/></param>
        /// <returns>The clone</returns>
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

        /// <summary>
        /// Determines if two <see cref="Uri"/>s references the same file. 
        /// </summary>
        /// <param name="uri1">The first <see cref="Uri"/></param>
        /// <param name="uri2">The second <see cref="Uri"/></param>
        /// <returns>A <see cref="bool"/> indicating if the two <see cref="Uri"/>s references the same file</returns>
        public static bool AreSameFile(Uri uri1, Uri uri2)
        {
            if (uri1 == null || uri2 == null)
            {
                return false;
            }

            if (uri1.IsAbsoluteUri != uri2.IsAbsoluteUri)
            {
                return false;
            }

            if (!uri1.IsAbsoluteUri)
            {
                uri1 = new Uri(new Uri("http://temp.org"), uri1);
                uri2 = new Uri(new Uri("http://temp.org"), uri2);
            }

            return uri2.GetLeftPart(UriPartial.Query).ToLowerInvariant() == uri1.GetLeftPart(UriPartial.Query).ToLowerInvariant();
        }

        /// <summary>
        /// Gets the duration of an audio file
        /// </summary>
        /// <param name="audioFile">The <see cref="Uri"/> of the audio file</param>
        /// <returns>The duration as a <see cref="TimeSpan"/></returns>
        public static TimeSpan GetAudioFileDuration(Uri audioFile)
        {
            var path = Uri.UnescapeDataString(audioFile.LocalPath);
            if (!File.Exists(path))
            {
                throw new InvalidOperationException($"Could not find audio file {audioFile}");
            }

            return new AudioFileReader(path).TotalTime;
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

            var meta = head.Elements(head.Name.Namespace + "meta").FirstOrDefault(m => m.Attribute("name")?.Value == name);
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

        /// <summary>
        /// Determines if an <see cref="Uri"/> is a ncc <see cref="Uri"/>
        /// </summary>
        /// <param name="uri">Tyhe <see cref="Uri"/></param>
        /// <returns><c>true</c> if <paramref name="uri"/> points to a file with name ncc.htm or ncc.html (case is ignored)</returns>
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
