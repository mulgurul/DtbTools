using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Speech.AudioFormat;
using System.Xml.Linq;
using System.Speech.Synthesis;
using NAudio.Wave;

namespace DtbSynthesizerLibrary
{
    public class SystemSpeechXmlSynthesizer : IXmlSynthesizer
    {
        static SystemSpeechXmlSynthesizer()
        {
            Synthesizer = new SpeechSynthesizer();
            SynthesizerList = new SpeechSynthesizer()
                .GetInstalledVoices()
                .Select(v => new SystemSpeechXmlSynthesizer(v.VoiceInfo))
                .ToList();
        }

        private static readonly List<SystemSpeechXmlSynthesizer> SynthesizerList;

        public static IReadOnlyCollection<IXmlSynthesizer> Synthesizers
            => SynthesizerList.AsReadOnly();

        public static IXmlSynthesizer GetPreferedVoiceForCulture(CultureInfo ci)
        {
            return Utils.GetPrefferedXmlSynthesizerForCulture(ci, Synthesizers);

        }

        private SystemSpeechXmlSynthesizer(VoiceInfo voice)
        {
            Voice = voice;
        }

        protected static SpeechSynthesizer Synthesizer { get; }

        protected VoiceInfo Voice { get; }

        protected TimeSpan Offset { get; set; }

        protected void AppendElementToPromptBuilder(XElement element, PromptBuilder promptBuilder, IDictionary<string, XElement> bookmarks)
        {
            var nameSuffix = bookmarks.Count.ToString("000000");
            bookmarks.Add(nameSuffix, element);
            promptBuilder.AppendBookmark($"B{nameSuffix}");
            foreach (var node in element.Nodes())
            {
                if (node is XElement elem)
                {
                    AppendElementToPromptBuilder(elem, promptBuilder, bookmarks);
                }
                else if (node is XText text)
                {
                    promptBuilder.AppendText(text.Value);
                }
            }
            promptBuilder.AppendBookmark($"E{nameSuffix}");
        }




        public TimeSpan SynthesizeElement(XElement element, WaveFileWriter writer, string src = "")
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            Offset = writer.TotalTime;
            var startOffset = Offset;
            var audioStream = new MemoryStream();
            Synthesizer.SetOutputToAudioStream(
                audioStream,
                new SpeechAudioFormatInfo(
                    writer.WaveFormat.SampleRate,
                    (AudioBitsPerSample)writer.WaveFormat.BitsPerSample,
                    (AudioChannel)writer.WaveFormat.Channels));
            var bookmarks = new Dictionary<string, XElement>();
            var promptBuilder = new PromptBuilder() { Culture = Voice.Culture };
            promptBuilder.StartVoice(Voice);
            AppendElementToPromptBuilder(element, promptBuilder, bookmarks);
            promptBuilder.EndVoice();
            var bookmarkDelegate = new EventHandler<BookmarkReachedEventArgs>((s, a) =>
            {
                if (bookmarks.ContainsKey(a.Bookmark.Substring(1)))
                {
                    var elem = bookmarks[a.Bookmark.Substring(1)];
                    var anno = elem.Annotation<SyncAnnotation>();
                    if (anno == null)
                    {
                        anno = new SyncAnnotation { Src = src };
                        elem.AddAnnotation(anno);
                    }

                    switch (a.Bookmark.Substring(0, 1))
                    {
                        case "B":
                            anno.ClipBegin = Offset;
                            break;
                        case "E":
                            Offset = startOffset + a.AudioPosition;
                            anno.ClipEnd = Offset;
                            break;
                    }

                }
            });
            Synthesizer.BookmarkReached += bookmarkDelegate;
            try
            {
                Synthesizer.Speak(promptBuilder);
            }
            finally
            {
                Synthesizer.BookmarkReached -= bookmarkDelegate;
            }
            Synthesizer.SetOutputToNull();
            writer.Write(audioStream.GetBuffer(), 0, (int)audioStream.Length);
            foreach (var anno in element.Annotations<SyncAnnotation>())
            {
                anno.ClipEnd = writer.TotalTime;
            }
            return writer.TotalTime.Subtract(startOffset);
        }

        public bool RecurseMixedContent { get; set; } = false;

        public VoiceMetaData VoiceInfo => new VoiceMetaData()
        {
            Name = Synthesizer.Voice.Name,
            Culture = Synthesizer.Voice.Culture,
            Gender = Synthesizer.Voice.Gender.ToString(),
            AdditionalInfo = new ReadOnlyDictionary<string, string>(Synthesizer.Voice.AdditionalInfo),
            Type = "System.Speech"
        };
    }
}
