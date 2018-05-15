using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace DtbMerger2Library.Daisy202
{
    public class MergeEntry
    {
        public Uri SourceNavEntry { get; set; }

        public IEnumerable<XElement> GetNccElements()
        {
            if (SourceNavEntry == null)
            {
                throw new InvalidOperationException("SourceNavEntry Uri is null");
            }
            if (!SourceNavEntry.IsAbsoluteUri)
            {
                throw new InvalidOperationException("SourceNavEntry Uri must be absolute");
            }
            if (String.IsNullOrEmpty(SourceNavEntry.Fragment))
            {
                throw new InvalidOperationException($"SourceNavEntry Uri {SourceNavEntry} has no fragment");
            }
            var heading = XDocument.Load(SourceNavEntry.AbsoluteUri, LoadOptions.SetBaseUri)
                .Descendants()
                .FirstOrDefault(e => $"#{e.Attribute("id")?.Value}" == SourceNavEntry.Fragment);
            if (heading == null)
            {
                throw new InvalidOperationException($"SourceNavEntry Uri {SourceNavEntry} fragment not foundt");
            }
            var res = new List<XElement>() {heading};
            var next = heading.ElementsAfterSelf().FirstOrDefault();
            while (next != null && !(new[] { "h1", "h2", "h3", "h4", "h5", "h6" }.Contains(next.Name.LocalName)))
            {
                res.Add(next);
                next = next.ElementsAfterSelf().FirstOrDefault();
            }
            return res;
        }

        public IEnumerable<XElement> GetSmilElements()
        {
            return new XElement[0];
        }

        public IEnumerable<XElement> GetTextElements()
        {
            return new XElement[0];
        }

        public IEnumerable<AudioSegment> GetAudioSegments()
        {
            return new AudioSegment[0];
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
