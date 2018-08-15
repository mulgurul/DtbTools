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
        public static IEnumerable<MergeEntry> LoadMergeEntriesFromNcc(XDocument nccDoc)
        {
            return nccDoc.Root
                ?.Element(nccDoc.Root.Name.Namespace + "body")
                ?.Elements(nccDoc.Root.Name.Namespace + "h1")
                .Select(LoadMergeEntryTreeFromHeading);
        }

        #region Static Methods
        public static IEnumerable<MergeEntry> LoadMergeEntriesFromNcc(Uri nccUri)
        {
            return LoadMergeEntriesFromNcc(
                XDocument.Load(
                    Uri.UnescapeDataString(nccUri.AbsoluteUri),
                    LoadOptions.SetBaseUri | LoadOptions.SetLineInfo));
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

        public static IEnumerable<MergeEntry> LoadMergeEntriesFromMacro(XDocument macroDoc)
        {
            return macroDoc.Root?.Elements().SelectMany(e => LoadMergeEntriesFromMacroElement(e)) ?? new MergeEntry[0];
        }

        public static IEnumerable<MergeEntry> LoadMergeEntriesFromMacro(Uri macroUri)
        {
            return LoadMergeEntriesFromMacro(
                XDocument.Load(
                    Uri.UnescapeDataString(macroUri.AbsoluteUri),
                    LoadOptions.SetBaseUri | LoadOptions.SetLineInfo));

        }

        public static IEnumerable<MergeEntry> LoadMergeEntriesFromMacroElement(XElement elem, bool deep = true)
        {
            var nccUri = Utils.GetUri(elem.Attribute("file"));
            if (nccUri == null)
            {
                throw new InvalidOperationException("Macro element does not have a file attribute with an Uri value");
            }
            ICollection<MergeEntry> res;
            if (elem.Attribute("ItemID") != null)
            {
                var nccElementUri = new UriBuilder(nccUri)
                {
                    Fragment = elem.Attribute("ItemID")?.Value ?? ""
                }.Uri;
                if (!Utils.IsNccUri(nccElementUri))
                {
                    nccElementUri = Utils.GetNccHeadingUriFromContentHeadingUri(nccElementUri);
                    if (nccElementUri == null)
                    {
                        throw new InvalidOperationException(
                            $"Could not find ncc heading for textual content heading {elem.Attribute("file")?.Value}#{elem.Attribute("ItemID")?.Value}");
                    }
                }

                res = new[] {new MergeEntry() {SourceNavEntry = nccElementUri}};
            }
            else//If ItemID attribute is missing, entire ncc is added
            {
                if (!Utils.IsNccUri(nccUri))
                {
                    throw new InvalidOperationException("Macro element without ItemID fragment specifier points no non-ncc document");
                }
                res = LoadMergeEntriesFromNcc(nccUri).ToList();
                if (Int32.TryParse(elem.Attribute("Skip")?.Value, out var skip))
                {
                    if (0 < skip && skip < res.Count)
                    {
                        while (skip > 0)
                        {
                            res.Remove(res.First());
                            skip--;
                        }
                    }
                }
            }

            if (deep)
            {
                res.Last().AddChildren(elem.Elements().SelectMany(e => LoadMergeEntriesFromMacroElement(e)));
            }
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
                    ncc = XDocument.Load(Uri.UnescapeDataString(SourceNavEntry.AbsoluteUri), LoadOptions.SetBaseUri|LoadOptions.SetLineInfo);
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
                                e.Descendants(e.Name.Namespace + "a").Select(a => 
                                    a.Attribute("href"))
                                        .Where(a => !String.IsNullOrWhiteSpace(a?.Value))
                                        .Select(Utils.GetUri))
                            .Select(uri => Uri.UnescapeDataString(uri.AbsoluteUri).ToLowerInvariant())
                            .Distinct()
                            .Select(path => 
                                XDocument.Load(
                                    path, 
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
                        .Select(uri => uri?.LocalPath)
                        .Where(path => !String.IsNullOrEmpty(path))
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
            var par = Utils.GetReferencedElement(hrefAttr, Smil);
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

        private static bool IsBodyBlockElement(XElement elem)
        {
            switch (elem.Parent?.Name.LocalName)
            {
                case "body":
                    return true;
                case "div":
                    return IsBodyBlockElement(elem.Parent);
                default:
                    return false;
            }
        }

        public IEnumerable<XElement> GetTextElements()
        {
            var textElements = GetSmilElements()
                .Select(par => par.Element(par.Name.Namespace + "text")?.Attribute("src"))
                .Select(Utils.GetUri)
                .Select(uri =>
                    Utils.GetReferencedElement(uri, ContentDocuments[uri.LocalPath]))
                .Select(elem => elem.AncestorsAndSelf().FirstOrDefault(IsBodyBlockElement))
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
            Path.GetExtension(GetAudioSegments().FirstOrDefault()?.AudioFile.LocalPath);

        public XElement MacroElement
        {
            get
            {
                return new XElement(
                    "Section",
                    new XAttribute("file", SourceNavEntry.LocalPath),
                    new XAttribute("ItemID", SourceNavEntry.Fragment.TrimStart('#')),
                    ChildNodes.Select(ch => ch.MacroElement));
            }
        }


        public override String ToString()
        {
            return SourceNavEntry.ToString();
        }
    }
}
