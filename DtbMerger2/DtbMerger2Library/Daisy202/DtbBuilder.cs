using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using NAudio.Lame;
using NAudio.Wave;

namespace DtbMerger2Library.Daisy202
{
    public class DtbBuilder
    {
        public TimeSpan AllowedFileEndAudio { get; set; } = TimeSpan.FromSeconds(1.5);

        public List<MergeEntry> MergeEntries { get; private set; }

        public DtbBuilder() : this(new MergeEntry[0])
        {
            MergeEntries = new List<MergeEntry>();
        }

        public DtbBuilder(IEnumerable<MergeEntry> entries)
        {
            MergeEntries = new List<MergeEntry>(entries);
            ContentDocumentName = "text.html";
            NccDocumentName = "ncc.html";
        }

        private string GetSmilFileName(int index)
        {
            return $"SM{index:D5}.smil";
        }

        public string AudioFileExtension =>
            Path.GetExtension(audioFileSegments.FirstOrDefault()?.FirstOrDefault()?.AudioFile.AbsolutePath.ToLowerInvariant());

        private string GetAudioFileName(int index)
        {
            return $"AUD{index:D5}{AudioFileExtension}";
        }

        private string GetImageFileName(Uri orgUri, int index)
        {
            return $"IMG{index:D5}_{Path.GetFileName(Uri.UnescapeDataString(orgUri.LocalPath))}";
        }

        private readonly List<XDocument> smilFiles = new List<XDocument>();

        private readonly List<List<AudioSegment>> audioFileSegments = new List<List<AudioSegment>>();

        public IDictionary<string, List<AudioSegment>> AudioFileSegments => Enumerable.Range(0, audioFileSegments.Count)
            .ToDictionary(GetAudioFileName, i => audioFileSegments[i]);

        public IDictionary<string, XDocument> SmilFiles => Enumerable.Range(0, smilFiles.Count)
            .ToDictionary(GetSmilFileName, i => smilFiles[i]);

        public string ContentDocumentName { get; private set; }

        public XDocument ContentDocument { get; private set; }

        public string NccDocumentName { get; private set; }

        public XDocument NccDocument { get; private set; }

        public IDictionary<string, XDocument> XmlDocuments => SmilFiles
            .Union(new[] {
                new KeyValuePair<String, XDocument>(NccDocumentName, NccDocument),
                new KeyValuePair<String, XDocument>(ContentDocumentName, ContentDocument)})
            .Where(kvp => kvp.Value != null)
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        public void ResetBuilder()
        {
            smilFiles.Clear();
            audioFileSegments.Clear();
            ContentDocument = null;
            NccDocument = Utils.GenerateSkeletonXhtmlDocument();
            entryIndex = 0;
            nccIdIndex = 0;
            contentIdIndex = 0;
            totalElapsedTime = TimeSpan.Zero;
        }

        private int entryIndex = 0;
        private int nccIdIndex = 0;
        private int contentIdIndex = 0;
        private TimeSpan totalElapsedTime = TimeSpan.Zero;

        private List<XElement> GetNccElements(MergeEntry me, List<XElement> smilElements)
        {
            var nccElements = me.GetNccElements().Select(Utils.CloneWithBaseUri).ToList();
            //Set new id attributes on ncc elements, fixing references from smil <text> elements
            foreach (var idAttr in nccElements
                .SelectMany(e => e.DescendantsAndSelf())
                .Select(e => e.Attribute("id"))
                .Where(attr => attr != null))
            {
                var newId = $"NCCID{nccIdIndex:D5}";
                nccIdIndex++;
                //Find and fix smil <text> elements src attributes, that link to ncc elements
                foreach (var textSrcAttr in smilElements
                    .Select(e => e.Element("text")?.Attribute("src"))
                    .Where(attr => Utils.IsReferenceTo(Utils.GetUri(attr), new Uri(me.Ncc.BaseUri))))
                {
                    var textSrcUri = Utils.GetUri(textSrcAttr);
                    if (textSrcUri.Fragment == $"#{idAttr.Value}")
                    {
                        textSrcAttr.Value = $"{NccDocumentName}#{newId}";
                    }
                }
                idAttr.Value = newId;
            }
            //Fix <a> element descendants of ncc elements, that point to the smil file
            foreach (var nccAHrefAttr in nccElements
                .SelectMany(e => e.DescendantsAndSelf(e.Name.Namespace + "a"))
                .Select(a => a.Attribute("href"))
                .Where(href => href != null))
            {
                var uri = Utils.GetUri(nccAHrefAttr);
                if (Utils.IsReferenceTo(uri, new Uri(me.Smil.BaseUri)))
                {
                    nccAHrefAttr.Value = $"{GetSmilFileName(entryIndex)}{uri.Fragment}";
                }
            }
            //Fix heading depth
            foreach (var hd in nccElements.Where(Utils.IsHeading))
            {
                hd.Name = hd.Name.Namespace + $"h{Math.Min(me.Depth, 6)}";
            }

            return nccElements;
        }

        public List<XElement> GetContentElements(MergeEntry me, List<XElement> smilElements)
        {
            var contentElements = me.GetTextElements().Select(Utils.CloneWithBaseUri).ToList();
            if (contentElements.Any())
            {
                //Set new id attributes on content elements, fixing references from smil <text> elements
                foreach (var idAttr in contentElements
                    .SelectMany(e => e.DescendantsAndSelf())
                    .Select(e => e.Attribute("id"))
                    .Where(attr => attr != null))
                {
                    var newId = $"TEXTID{contentIdIndex:D5}";
                    contentIdIndex++;
                    //Find and fix smil <text> elements src attributes, that link to content elements
                    foreach (var textSrcAttr in smilElements
                        .Select(e => e.Element("text")?.Attribute("src"))
                        .Where(attr => me.ContentDocuments.Values.Any(cd =>
                            Utils.IsReferenceTo(Utils.GetUri(attr), new Uri(cd.BaseUri)))))
                    {
                        var textSrcUri = Utils.GetUri(textSrcAttr);
                        if (textSrcUri.Fragment == $"#{idAttr.Value}")
                        {
                            textSrcAttr.Value = $"{ContentDocumentName}#{newId}";
                        }
                    }
                    idAttr.Value = newId;
                }
                //Fix <a> element descendants of ncc elements, that point to the smil file
                foreach (var contentAHrefAttr in contentElements
                    .SelectMany(e => e.DescendantsAndSelf(e.Name.Namespace + "a"))
                    .Select(a => a.Attribute("href"))
                    .Where(href => href != null))
                {
                    var uri = Utils.GetUri(contentAHrefAttr);
                    if (Utils.IsReferenceTo(uri, new Uri(me.Smil.BaseUri)))
                    {
                        contentAHrefAttr.Value = $"{GetSmilFileName(entryIndex)}{uri.Fragment}";
                    }
                }
            }

            return contentElements;
        }

        public void BuildDtb()
        {
            if (!MergeEntries.Any())
            {
                throw new InvalidOperationException("No merge entries added to builder");
            }

            ResetBuilder();
            var generator =
                $"{Assembly.GetExecutingAssembly().GetName().Name} v{Assembly.GetExecutingAssembly().GetName().Version}";

            var entries = MergeEntries.SelectMany(entry => entry.DescententsAndSelf).ToList();
            if (entries.Any(me => me.GetTextElements().Any()))
            {
                ContentDocument = Utils.GenerateSkeletonXhtmlDocument();
            }

            var identifier = Utils.CreateOrGetMeta(entries.First().Ncc, "dc:identifier")?.Attribute("content")?.Value ??
                             Guid.NewGuid().ToString();
            foreach (var me in entries)
            {
                var smilFile = Utils.GenerateSkeletonSmilDocument();
                var smilElements = me.GetSmilElements().Select(Utils.CloneWithBaseUri).ToList();
                NccDocument.Root?.Element(NccDocument?.Root.Name.Namespace + "body")
                    ?.Add(GetNccElements(me, smilElements));

                var contentElements = GetContentElements(me, smilElements);
                if (contentElements.Any())
                {
                    var firstHeading = contentElements.FirstOrDefault(Utils.IsHeading);
                    if (firstHeading != null)
                    {
                        firstHeading.Name = firstHeading.Name.Namespace + $"h{Math.Min(me.Depth, 6)}";
                    }

                    ContentDocument.Root?.Element(ContentDocument?.Root.Name.Namespace + "body")?.Add(contentElements);
                }

                audioFileSegments.Add(me.GetAudioSegments().ToList());
                var elapsedInThisSmil = TimeSpan.Zero;
                foreach (var audio in smilElements.Descendants("audio"))
                {
                    var clipBegin = Utils.ParseSmilClip(audio.Attribute("clip-begin")?.Value);
                    var clipEnd = Utils.ParseSmilClip(audio.Attribute("clip-end")?.Value);
                    audio.SetAttributeValue("src", GetAudioFileName(entryIndex));
                    audio.SetAttributeValue(
                        "clip-begin",
                        $"npt={elapsedInThisSmil.TotalSeconds.ToString("F3", CultureInfo.InvariantCulture)}s");
                    elapsedInThisSmil += clipEnd.Subtract(clipBegin);
                    audio.SetAttributeValue(
                        "clip-end",
                        $"npt={elapsedInThisSmil.TotalSeconds.ToString("F3", CultureInfo.InvariantCulture)}s");
                }

                var timeInThisSmil = TimeSpan.FromSeconds(smilElements
                    .SelectMany(e => e.Descendants("audio"))
                    .Select(audio => Utils
                        .ParseSmilClip(audio.Attribute("clip-end")?.Value)
                        .Subtract(Utils.ParseSmilClip(audio.Attribute("clip-begin")?.Value)).TotalSeconds)
                    .Sum());
                Utils.CreateOrGetMeta(smilFile, "ncc:totalElapsedTime")?.SetAttributeValue(
                    "content", Utils.GetHHMMSSFromTimeSpan(totalElapsedTime));
                Utils.CreateOrGetMeta(smilFile, "ncc:timeInThisSmil")?.SetAttributeValue(
                    "content", Utils.GetHHMMSSFromTimeSpan(timeInThisSmil));
                var seq = smilFile.Root?.Element("body")?.Element("seq");
                if (seq == null)
                {
                    throw new ApplicationException("Generated smil document contains no main seq");
                }

                seq.SetAttributeValue(
                    "dur",
                    $"{timeInThisSmil.TotalSeconds.ToString("F3", CultureInfo.InvariantCulture)}s");
                Utils.CreateOrGetMeta(smilFile, "ncc:generator")?.SetAttributeValue("content", generator);
                Utils.CreateOrGetMeta(smilFile, "dc:identifier")?.SetAttributeValue("content", identifier);
                seq.Add(smilElements);
                smilFiles.Add(smilFile);
                totalElapsedTime += TimeSpan.FromSeconds(Math.Ceiling(timeInThisSmil.TotalSeconds));
                foreach (var imgSrc in contentElements.SelectMany(ce =>
                    ce.DescendantsAndSelf(Utils.XhtmlNs + "img").Select(img => img.Attribute("src"))
                        .Where(src => src != null)))
                {
                    imgSrc.Value = GetImageFileName(Utils.GetUri(imgSrc), entryIndex);
                }

                entryIndex++;
            }

            NccDocument.Root?.Element(Utils.XhtmlNs + "head")?.Add(
                entries.First().Ncc.Root?.Element(Utils.XhtmlNs + "head")?.Elements(Utils.XhtmlNs + "meta"));

            Utils.CreateOrGetMeta(NccDocument, "ncc:totalTime")
                ?.SetAttributeValue("content", Utils.GetHHMMSSFromTimeSpan(totalElapsedTime));
            var fileCount =
                1
                + 2 * smilFiles.Count
                + (ContentDocument == null ? 0 : 1)
                + ContentDocument
                    ?.Descendants(Utils.XhtmlNs + "img")
                    .Select(img => img.Attribute("src")?.Value)
                    .Distinct().Count(src => !String.IsNullOrWhiteSpace(src));
            Utils.CreateOrGetMeta(NccDocument, "ncc:files")?.SetAttributeValue("content", fileCount);
            Utils.CreateOrGetMeta(NccDocument, "ncc:depth")?.SetAttributeValue(
                "content",
                entries.Select(me => me.Depth).Max());
            Utils.CreateOrGetMeta(NccDocument, "ncc:tocItems")?.SetAttributeValue(
                "content",
                entries.Count);
            foreach (var pt in new[] {"Front", "Normal", "Special"})
            {
                Utils.CreateOrGetMeta(NccDocument, $"ncc:page{pt}")?.SetAttributeValue(
                    "content",
                    entries.SelectMany(me =>
                            me.GetNccElements()
                                .Where(e =>
                                    e.Name == Utils.XhtmlNs + "span"
                                    && e.Attribute("class")?.Value == $"page-{pt.ToLowerInvariant()}"))
                        .Count());
            }

            Utils.CreateOrGetMeta(NccDocument, "ncc:multimediaType")?.SetAttributeValue(
                "content",
                ContentDocument == null ? "audioNCC" : "audioFullText");
            Utils.CreateOrGetMeta(NccDocument, "ncc:generator")?.SetAttributeValue("content", generator);
            //Remove whitespace only text nodes in smil files
            foreach (var whiteSpace in SmilFiles.Values
                .SelectMany(doc => doc.DescendantNodes().OfType<XText>())
                .Where(text => String.IsNullOrWhiteSpace(text.Value))
                .ToList())

            {
                whiteSpace.Remove();
            }

            //Remove whitespace at the start or end of h1-6 and p elements
            foreach (var elem in new[] { NccDocument, ContentDocument }.Where(doc => doc != null)
                .SelectMany(doc => doc.Descendants())
                .Where(Utils.IsHeadingOrParagraph)
                .ToList())
            {
                foreach (var text in elem.DescendantNodes().OfType<XText>())
                {
                    text.Value = Regex.Replace(text.Value, @"\s+", " ");
                }
                TrimWhitespace(elem);
            }
        }

        private void TrimWhitespace(XElement elem)
        {
            if (elem.FirstNode is XText firstNode)
            {
                if (String.IsNullOrWhiteSpace(firstNode.Value))
                {
                    firstNode.Remove();
                }
                else
                {
                    firstNode.Value = firstNode.Value.TrimStart();
                }
                
            }
            if (elem.LastNode is XText lastNode)
            {
                if (String.IsNullOrWhiteSpace(lastNode.Value))
                {
                    lastNode.Remove();
                }
                else
                {
                    lastNode.Value = lastNode.Value.TrimEnd();
                }
            }

            if (elem.Nodes().Count() == 1 && elem.Elements().Count() == 1)
            {
                TrimWhitespace(elem.Elements().First());
            }
        }

        public void SaveDtb(string baseDir)
        {
            if (Directory.Exists(baseDir))
            {
                foreach (var dir in Directory.GetDirectories(baseDir))
                {
                    Directory.Delete(dir, true);
                }

                foreach (var file in Directory.GetFiles(baseDir))
                {
                    File.Delete(file);
                }
            }
            Directory.CreateDirectory(baseDir);
            var xmlDocs = XmlDocuments;
            var wrSettings = new XmlWriterSettings()
            {
                Indent = true,
                IndentChars = "\t",
                NamespaceHandling = NamespaceHandling.OmitDuplicates
            };
            foreach (var xmlFileName in xmlDocs.Keys)
            {
                using (var writer = XmlWriter.Create(Path.Combine(baseDir, xmlFileName), wrSettings))
                {
                    xmlDocs[xmlFileName].Save(writer);
                }
            }
            SaveAudioFiles(baseDir);
            SaveMediaFiles(baseDir);
        }

        private WaveStream GetAudioPcmStream(string path)
        {
            switch (Path.GetExtension(path)?.ToLowerInvariant())
            {
                case ".mp3":
                    return new Mp3FileReader(path);
                case ".wav":
                    return new WaveFileReader(path);
                default:
                    throw new NotSupportedException($"Audio file extension {Path.GetExtension(path)?.ToLowerInvariant()} is not supported");
            }
        }

        private void SaveAudioFiles(string baseDir)
        {
            foreach (var audioFileName in AudioFileSegments.Keys.Where(key => AudioFileSegments[key].Any()))
            {
                if (AudioFileSegments[audioFileName].Count == 1)
                {
                    var audSeg = AudioFileSegments[audioFileName].First();
                    if (audSeg.AudioFileDuration < audSeg.ClipEnd)
                    {
                        throw new InvalidOperationException(
                            $"Audio segment clip-end {audSeg.ClipEnd} is beyond the end of audio file {audSeg.AudioFile}");
                    }
                    if (audSeg.AudioFileDuration < audSeg.ClipEnd.Add(AllowedFileEndAudio))
                    {
                        File.Copy(Uri.UnescapeDataString(AudioFileSegments[audioFileName][0].AudioFile.LocalPath), Path.Combine(baseDir, audioFileName));
                        continue;
                    }
                }

                Stream underlyingStream = new FileStream(Path.Combine(baseDir, audioFileName), FileMode.Create, FileAccess.ReadWrite);
                byte[] byteSuffix = null;
                try
                {
                    Stream audioStream;
                    WaveFormat waveFormat;
                    var firstAudioPath = Uri.UnescapeDataString(
                        AudioFileSegments[audioFileName].First().AudioFile.LocalPath);
                    switch (AudioFileExtension)
                    {
                        case ".mp3":
                            using (var mp3FR = new Mp3FileReader(firstAudioPath))
                            {
                                waveFormat = mp3FR.WaveFormat;
                                audioStream = new LameMP3FileWriter(
                                    underlyingStream, 
                                    waveFormat, 
                                    mp3FR.Mp3WaveFormat.AverageBytesPerSecond/8);
                                if (mp3FR.Id3v2Tag != null)
                                {
                                    underlyingStream.Write(mp3FR.Id3v2Tag.RawData, 0, mp3FR.Id3v2Tag.RawData.Length);
                                }

                                byteSuffix = mp3FR.Id3v1Tag.ToArray();
                            }
                            break;
                        case ".wav":
                            waveFormat = new WaveFileReader(firstAudioPath).WaveFormat;
                            audioStream = new WaveFileWriter(underlyingStream, new WaveFileReader(firstAudioPath).WaveFormat);
                            break;
                        default:
                            throw new NotSupportedException($"Audio file extension {AudioFileExtension} is not supported");
                    }

                    try
                    {
                        foreach (var segment in AudioFileSegments[audioFileName])
                        {
                            using (var audioReader = 
                                GetAudioPcmStream(Uri.UnescapeDataString(segment.AudioFile.LocalPath)))
                            {
                                if (!waveFormat.Equals(audioReader.WaveFormat))
                                {
                                    throw new NotSupportedException(
                                        $"Audio file {segment.AudioFile} has different wave format from first audio file in segment");
                                }
                                audioReader.Seek(
                                    (long)segment.ClipBegin.TotalSeconds * audioReader.WaveFormat.AverageBytesPerSecond,
                                    SeekOrigin.Current);
                                var bytesToRead =
                                    (long) (segment.Duration.TotalSeconds * audioReader.WaveFormat.AverageBytesPerSecond);
                                var bytesRead = 0;
                                var buf = new Byte[10*1024];
                                while (bytesRead < bytesToRead)
                                {
                                    int count = (int)Math.Min(bytesToRead - bytesRead, buf.Length);
                                    bytesRead += audioReader.Read(buf, 0, count);
                                    audioStream.Write(buf, 0, count);
                                }
                            }
                        }

                    }
                    finally
                    {
                        audioStream?.Flush();
                    }
                }
                finally
                {
                    if (byteSuffix != null)
                    {
                        underlyingStream.Write(byteSuffix, 0, byteSuffix.Length);
                    }
                    underlyingStream.Close();
                }
            }
        }

        private void SaveMediaFiles(string baseDir)
        {
            int index = 0;
            foreach (var entry in MergeEntries.SelectMany(me => me.DescententsAndSelf))
            {
                foreach (var imgUri in entry.GetTextElements()
                    .SelectMany(e => e.DescendantsAndSelf(e.Name.Namespace + "img").Select(img => img.Attribute("src")))
                    .Select(Utils.GetUri)
                    .Distinct())
                {
                    File.Copy(
                        Uri.UnescapeDataString(imgUri.LocalPath),
                        Path.Combine(baseDir, GetImageFileName(imgUri, index)));
                }
                index++;
            }
        }
    }
}
