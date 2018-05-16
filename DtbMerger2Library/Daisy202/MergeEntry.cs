using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;

namespace DtbMerger2Library.Daisy202
{
    public class MergeEntry
    {
        private static readonly String[] HeadingLocalNames = new[] {"h1", "h2", "h3", "h4", "h5", "h6"};

        private XDocument ncc, smil;
        public Uri SourceNavEntry { get; set; }

        public XDocument Ncc
        {
            get
            {
                if (ncc == null)
                {
                    if (SourceNavEntry == null)
                    {
                        throw new InvalidOperationException("SourceNavEntry Uri is null");
                    }
                    if (!SourceNavEntry.IsAbsoluteUri)
                    {
                        throw new InvalidOperationException("SourceNavEntry Uri must be absolute");
                    }
                    ncc = XDocument.Load(Uri.UnescapeDataString(SourceNavEntry.AbsolutePath), LoadOptions.SetBaseUri|LoadOptions.SetLineInfo);
                }
                return ncc;
            }
        }

        public XDocument Smil
        {
            get
            {
                if (smil == null)
                {
                    try
                    {
                        smil = GetNccElements()
                            .SelectMany(e =>
                                e.Descendants(e.Name.Namespace + "a").Select(a => a.Attribute("href")?.Value.ToLowerInvariant()))
                            .Distinct()
                            .Where(href => !String.IsNullOrEmpty(href))
                            .Select(href => 
                                XDocument.Load(
                                    Uri.UnescapeDataString(new Uri(new Uri(ncc.BaseUri), href).AbsolutePath), 
                                    LoadOptions.SetBaseUri | LoadOptions.SetLineInfo))
                            .SingleOrDefault();
                    }
                    catch (InvalidOperationException e)
                    {
                        throw new InvalidOperationException("NCC elements of MergeEntry links to multiple smil files", e);
                    }
                }
                return smil;
            }
        }

        public IEnumerable<XElement> GetNccElements()
        {
            if (String.IsNullOrEmpty(SourceNavEntry?.Fragment))
            {
                throw new InvalidOperationException($"SourceNavEntry Uri {SourceNavEntry} has no fragment");
            }
            var heading = Ncc
                .Descendants()
                .FirstOrDefault(e => $"#{e.Attribute("id")?.Value}" == SourceNavEntry.Fragment);
            if (heading == null)
            {
                throw new InvalidOperationException($"SourceNavEntry Uri {SourceNavEntry} fragment not foundt");
            }
            var res = new List<XElement>() {heading};
            var next = heading.ElementsAfterSelf().FirstOrDefault();
            while (next != null && !(HeadingLocalNames.Contains(next.Name.LocalName)))
            {
                res.Add(next);
                next = next.ElementsAfterSelf().FirstOrDefault();
            }
            return res;
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

        private XElement GetSmilPar(XElement elem)
        {
            var hrefAttr = elem
                .Descendants()
                .FirstOrDefault(e => e.Name.LocalName == "a")
                ?.Attribute("href");
            if (hrefAttr == null)
            {
                return null;
            }
            var par = GetReferencedElement(Smil, hrefAttr);
            if (par?.Name.LocalName == "text")
            {
                par = par.Parent;
            }

            return par;
        }

        public IEnumerable<XElement> GetSmilElements()
        {
            if (Smil == null)
            {
                return new XElement[0];
            }
            var nccElements = GetNccElements().ToList();
            var firstPar = GetSmilPar(nccElements.First());
            if (firstPar == null)
            {
                throw new InvalidOperationException("First ncc element does not point to smil par");
            }
            var nextHeadingPar = GetSmilPar(
                nccElements.Last().ElementsAfterSelf().FirstOrDefault(e => HeadingLocalNames.Contains(e.Name.LocalName)));
            var res = new List<XElement>();
            while (firstPar != null)
            {
                if (firstPar == nextHeadingPar)
                {
                    break;
                }
                res.Add(firstPar);
                firstPar = firstPar.ElementsAfterSelf().FirstOrDefault(e => e.Name.LocalName == "par");
            }

            return res;
        }

        public IEnumerable<XElement> GetTextElements()
        {
            var contentDocs = new Dictionary<string, XDocument>();
            var textElements = GetSmilElements()
                .Select(par => par.Element(par.Name.Namespace + "text")?.Attribute("src"))
                .Select(srcAttr =>
                {
                    var uri = GetUri(srcAttr);
                    var path = uri?.AbsolutePath;
                    if (String.IsNullOrEmpty(path))
                    {
                        return null;
                    }
                    if (!contentDocs.ContainsKey(path))
                    {
                        contentDocs.Add(
                            path,
                            XDocument.Load(Uri.UnescapeDataString(path), LoadOptions.SetBaseUri|LoadOptions.SetLineInfo));
                    }
                    return GetReferencedElement(contentDocs[path], uri);
                })
                .Select(elem => elem.AncestorsAndSelf().FirstOrDefault(e => e.Parent?.Name?.LocalName == "body"))
                .Where(e => e != null)
                .Distinct()
                .ToList();
            int i = 0;
            while (i < textElements.Count-1)
            {
                var e1 = textElements[i];
                var e2 = textElements[i + 1];
                var siblingsBetween = e1.ElementsAfterSelf().Intersect(e2.ElementsBeforeSelf()).ToList();
                if (siblingsBetween.Any())
                {
                    textElements.InsertRange(i+1, siblingsBetween);
                    i += siblingsBetween.Count;
                }
                i++;
            }
            return textElements;
        }

        public IEnumerable<AudioSegment> GetAudioSegments()
        {
            var audios = GetSmilElements()
                .SelectMany(par => par.Descendants(par.Name.Namespace + "audio"))
                .ToList();
            int i = 0;
            while (i < audios.Count - 1)
            {
                var a1 = audios[i];
                var a2 = audios[i + 1];
                if (a1.Attribute("src")?.Value == a2.Attribute("src")?.Value)
                {
                    if (a1.Attribute("clip-end")?.Value == a2.Attribute("clip-begin")?.Value)
                    {
                        a1.SetAttributeValue("clip-end", a2.Attribute("clip-end")?.Value);
                        audios.Remove(a2);
                        continue;
                    }
                }
                i++;
            }

            return audios
                .Where(audio => !String.IsNullOrEmpty(audio.Attribute("src")?.Value))
                .Select(audio => new AudioSegment()
                {
                    AudioFile = new Uri(new Uri(audio.BaseUri), audio.Attribute("src")?.Value??""),
                    ClipBegin = ParseSmilClip(audio.Attribute("clip-begin")?.Value),
                    ClipEnd = ParseSmilClip(audio.Attribute("clip-end")?.Value)
                });
        }

        private static TimeSpan ParseSmilClip(string val)
        {
            if (String.IsNullOrWhiteSpace(val))
            {
                return TimeSpan.MinValue;
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
                throw new ArgumentException($"Value {val} is not a valid Daisy 2.02 smil clop value");
            }
        }

        public IEnumerable<MediaEntry> GetMediaEntries()
        {
            var res = GetTextElements()
                .SelectMany(
                    n => n.DescendantNodesAndSelf()
                        .OfType<XElement>()
                        .Where(e => e.Name.LocalName == "img")
                        .Select(e => e.Attribute("src")?.Value.ToLowerInvariant())
                        .Where(relUri => Uri.IsWellFormedUriString(relUri, UriKind.Relative))
                        .Select(relUri => new MediaEntry(){RelativeUri = new Uri(relUri), Source = new Uri(new Uri(n.BaseUri), relUri)}))
                .Distinct();
            return res;
        }
    }
}
