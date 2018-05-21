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
                            new XAttribute("charset", "utf-8")),
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

            return dest.AbsolutePath == source.AbsolutePath;
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
            return TimeSpan.FromSeconds(Math.Ceiling(val.TotalSeconds)).ToString(@"hh\:mm\:ss");
        }
    }
}
