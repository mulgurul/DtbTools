using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Xml.XPath;
using DtbMerger2Library.Tree;

namespace DtbMerger2Library.Daisy202
{
    public class MergeEntry : TreeNode<MergeEntry>
    {
        #region Static Methods
        public static IEnumerable<MergeEntry> LoadMergeEntriesFromNcc(Uri nccUri)
        {
            var ncc = XDocument.Load(Uri.UnescapeDataString(nccUri.AbsolutePath), LoadOptions.SetBaseUri | LoadOptions.SetLineInfo);
            return ncc.Root
                ?.Element(ncc.Root.Name.Namespace + "body")
                ?.Elements(ncc.Root.Name.Namespace + "h1")
                .Select(LoadMergeEntryTreeFromHeading);
        }

        public static MergeEntry LoadMergeEntryTreeFromHeading(XElement heading)
        {
            if (heading == null) throw new ArgumentNullException(nameof(heading));
            if (String.IsNullOrEmpty(heading.Attribute("id")?.Value))
            {
                throw new ArgumentException("Heading element has no id attribute", nameof(heading));
            }

            var res = new MergeEntry()
            {
                SourceNavEntry = new UriBuilder(new Uri(heading.BaseUri)) { Fragment = heading.Attribute("id")?.Value ?? "" }.Uri
            };
            var subHeadingName = Utils.GetSubHeadingName(heading.Name);
            if (subHeadingName != null)
            {
                var followingSubHeadings = heading.ElementsAfterSelf(subHeadingName);
                var followingHeading = heading.ElementsAfterSelf(heading.Name).FirstOrDefault();
                if (followingHeading != null)
                {
                    followingSubHeadings = followingSubHeadings.Intersect(followingHeading.ElementsBeforeSelf());
                }
                res.AddChildren(followingSubHeadings.Select(LoadMergeEntryTreeFromHeading));
            }
            return res;
        }

        public static IEnumerable<MergeEntry> LoadMergeEntriesFromMacro(Uri macroUri)
        {
            return XDocument
                       .Load(Uri.UnescapeDataString(macroUri.AbsolutePath), LoadOptions.SetBaseUri | LoadOptions.SetLineInfo)
                       .Root?.Elements().Select(LoadMergeEntryFromMacroElement) ?? new MergeEntry[0];
        }

        public static MergeEntry LoadMergeEntryFromMacroElement(XElement elem)
        {
            var res = new MergeEntry()
            {
                SourceNavEntry = new UriBuilder(new Uri(elem.Attribute("file")?.Value ?? "")) { Fragment = elem.Attribute("ItemID")?.Value ?? "" }.Uri
            };
            res.AddChildren(elem.Elements().Select(LoadMergeEntryFromMacroElement));
            return res;
        }
        #endregion

        private Uri sourceNavEntry;
        private XDocument ncc, smil;
        private Dictionary<string, XDocument> contentDocuments;

        public Uri SourceNavEntry
        {
            get => sourceNavEntry;
            set
            {
                if (sourceNavEntry != value)
                {
                    ncc = null;
                    smil = null;
                    contentDocuments = null;
                }
                sourceNavEntry = value;
            }
        }

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

        public IReadOnlyDictionary<string, XDocument> ContentDocuments
        {
            get
            {
                if (contentDocuments == null)
                {
                    contentDocuments = new Dictionary<String, XDocument>();
                    foreach (var path in GetSmilElements()
                        .Select(par => par.Element(par.Name.Namespace + "text")?.Attribute("src"))
                        .Select(Utils.GetUri)
                        .Select(uri => uri?.AbsolutePath)
                        .Where(path => !String.IsNullOrEmpty(path))
                        .Select(Uri.UnescapeDataString)
                        .Distinct())
                    {
                        contentDocuments.Add(path, XDocument.Load(path, LoadOptions.SetBaseUri | LoadOptions.SetLineInfo));
                    }
                }
                return new ReadOnlyDictionary<String, XDocument>(contentDocuments);
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
                throw new InvalidOperationException($"SourceNavEntry Uri {SourceNavEntry} fragment not found");
            }
            var res = new List<XElement>() {heading};
            var next = heading.ElementsAfterSelf().FirstOrDefault();
            while (next != null && !Utils.IsHeading(next))
            {
                res.Add(next);
                next = next?.ElementsAfterSelf().FirstOrDefault();
            }
            return res;
        }

        private XElement GetSmilPar(XElement elem)
        {
            var hrefAttr = elem
                ?.Descendants()
                .FirstOrDefault(e => e.Name.LocalName == "a")
                ?.Attribute("href");
            if (hrefAttr == null)
            {
                return null;
            }
            var par = Utils.GetReferencedElement(Smil, hrefAttr);
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
                nccElements.Last().ElementsAfterSelf().FirstOrDefault(Utils.IsHeading));
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
            var textElements = GetSmilElements()
                .Select(par => par.Element(par.Name.Namespace + "text")?.Attribute("src"))
                .Select(Utils.GetUri)
                .Select(uri =>
                    Utils.GetReferencedElement(ContentDocuments[Uri.UnescapeDataString(uri.AbsolutePath)], uri))
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
                    ClipBegin = Utils.ParseSmilClip(audio.Attribute("clip-begin")?.Value),
                    ClipEnd = Utils.ParseSmilClip(audio.Attribute("clip-end")?.Value)
                });
        }

        public string DefaultAudioFileExtension =>
            Path.GetExtension(GetAudioSegments().FirstOrDefault()?.AudioFile.AbsolutePath);

        public IEnumerable<MediaEntry> GetMediaEntries()
        {
            var res = GetTextElements()
                .SelectMany(
                    n => n.DescendantNodesAndSelf()
                        .OfType<XElement>()
                        .Where(e => e.Name.LocalName == "img")
                        .Select(e => e.Attribute("src")?.Value.ToLowerInvariant())
                        .Where(relUri => Uri.IsWellFormedUriString(relUri, UriKind.Relative))
                        .Select(relUri => new MediaEntry(){RelativeUri = new Uri(relUri, UriKind.Relative), Source = new Uri(new Uri(n.BaseUri), relUri)}))
                .Distinct();
            return res;
        }

        public override String ToString()
        {
            return SourceNavEntry.ToString();
        }
    }
}
