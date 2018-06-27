using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Speech.AudioFormat;
using Microsoft.Speech.Synthesis;

namespace DtbSynthesizerLibrary
{
    public class MSSpeechXmlSynthesizer : IXmlSynthesizer
    {
        private String waveFile = "";
        private SpeechAudioFormatInfo audioFormat = new SpeechAudioFormatInfo(22050, AudioBitsPerSample.Sixteen, AudioChannel.Mono);
        protected SpeechSynthesizer Synthesizer { get; }

        public MSSpeechXmlSynthesizer()
        {
            Synthesizer = new SpeechSynthesizer();
            SelectVoice(null);
        }

        public string SelectVoice(CultureInfo ci)
        {
            if (ci == null)
            {
                ci = CultureInfo.CurrentCulture;
            }
            InstalledVoice voice = null;
            if (!ci.IsNeutralCulture)
            {
                voice = Synthesizer.GetInstalledVoices(ci).FirstOrDefault(v => v.Enabled);
            }

            if (voice == null)
            {
                voice = Synthesizer
                    .GetInstalledVoices()
                    .FirstOrDefault(v => v.Enabled && v.VoiceInfo.Culture.TwoLetterISOLanguageName == ci.TwoLetterISOLanguageName);
            }

            if (voice == null)
            {
                voice = Synthesizer.GetInstalledVoices().First(v => v.Enabled);
            }
            Synthesizer.SelectVoice(voice.VoiceInfo.Name);
            return voice.VoiceInfo.Name;
        }

        public IEnumerable<string> VoiceNames => Synthesizer.GetInstalledVoices().Select(v => v.VoiceInfo.Name);

        public SpeechAudioFormatInfo AudioFormat
        {
            get => audioFormat;
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException($"{nameof(AudioFormat)} cannot be null");
                }

                if (!new SpeechAudioFormatInfoEqualityComparer().Equals(value, audioFormat))
                {
                    audioFormat = value;
                    var wf = WaveFile;
                    WaveFile = "";
                    WaveFile = wf;
                }
            }
        }

        private class SpeechAudioFormatInfoEqualityComparer : IEqualityComparer<SpeechAudioFormatInfo>
        {
            public Boolean Equals(SpeechAudioFormatInfo x, SpeechAudioFormatInfo y)
            {
                if (x == null || y == null)
                {
                    return (x == null) == (y == null);
                }

                return
                    x.SamplesPerSecond == y.SamplesPerSecond
                    && x.BitsPerSample == y.BitsPerSample
                    && x.ChannelCount == y.ChannelCount
                    && x.EncodingFormat == y.EncodingFormat;
            }

            public Int32 GetHashCode(SpeechAudioFormatInfo obj)
            {
                return obj.SamplesPerSecond.GetHashCode() 
                       & obj.BitsPerSample.GetHashCode() 
                       & obj.ChannelCount.GetHashCode() 
                       & obj.EncodingFormat.GetHashCode();
            }
        }

        public String WaveFile
        {
            get => waveFile;
            set
            {
                var val = String.IsNullOrWhiteSpace(value)?"":value.Trim();
                if (!String.Equals(val, waveFile, StringComparison.InvariantCultureIgnoreCase))
                {
                    waveFile = val;
                    Offset = TimeSpan.Zero;
                    if (String.IsNullOrWhiteSpace(waveFile))
                    {
                        Synthesizer.SetOutputToNull();
                    }
                    else
                    {
                        Synthesizer.SetOutputToWaveFile(waveFile, AudioFormat);
                    }
                }
            }
        }

        public TimeSpan Offset { get; protected set; } = TimeSpan.Zero;

        protected PromptBuilder GetPromptBuilder(XElement element, IDictionary<string, XElement> bookmarks, Func<XElement, CultureInfo> languageSelector)
        {
            var promptBuilder = new PromptBuilder
            {
                Culture = (languageSelector??(xElement => Synthesizer.Voice.Culture))(element)
            };
            
            var nameSuffix = bookmarks.Count.ToString("000000");
            bookmarks.Add(nameSuffix, element);
            promptBuilder.AppendBookmark($"B{nameSuffix}");
            foreach (var node in element.Nodes())
            {
                switch (node.NodeType)
                {
                    case XmlNodeType.Element:
                        promptBuilder.AppendPromptBuilder(GetPromptBuilder((XElement)node, bookmarks, languageSelector));
                        break;
                    case XmlNodeType.Text:
                        promptBuilder.AppendText(((XText)node).Value);
                        break;
                }
            }
            promptBuilder.AppendBookmark($"E{nameSuffix}");
            return promptBuilder;
        }

        public TimeSpan SynthesizeElement(XElement element, Func<XElement, CultureInfo> languageSelector)
        {
            var bookmarks = new Dictionary<string, XElement>();
            var prompt = GetPromptBuilder(element, bookmarks, languageSelector);
            var bookmarkDelegate = new EventHandler<BookmarkReachedEventArgs>((s, a) =>
            {
                if (bookmarks.ContainsKey(a.Bookmark.Substring(1)))
                {
                    var elem = bookmarks[a.Bookmark.Substring(1)];
                    var anno = elem.Annotation<SyncAnnotation>();
                    if (anno == null)
                    {
                        anno = new SyncAnnotation();
                        elem.AddAnnotation(anno);
                    }

                    switch (a.Bookmark.Substring(0, 1))
                    {
                        case "B":
                            anno.ClipBegin = Offset;
                            break;
                        case "E":
                            Offset = a.AudioPosition;
                            anno.ClipEnd = Offset;
                            break;
                    }
                    
                }
            });
            Synthesizer.BookmarkReached += bookmarkDelegate;
            try
            {
                Synthesizer.Speak(prompt);
            }
            finally
            {
                Synthesizer.BookmarkReached -= bookmarkDelegate;
            }

            var elemAnno = element.Annotation<SyncAnnotation>();
            if (elemAnno == null)
            {
                throw new ApplicationException("No SyncAnnotation on element");
            }
            var dur = elemAnno.ClipEnd.Subtract(elemAnno.ClipBegin);
            return dur;
        }

        public IEnumerable<VoiceMetaData> GetVoices()
        {
            return Synthesizer.GetInstalledVoices().Where(v => v.Enabled).Select(v =>
                new VoiceMetaData()
                {
                    Name = v.VoiceInfo.Name,
                    Culture = v.VoiceInfo.Culture,
                    Gender = v.VoiceInfo.Gender,
                    AdditionalInfo = new ReadOnlyDictionary<String, String>(v.VoiceInfo.AdditionalInfo)
                });
        }

        public bool IsVoiceSupported(VoiceMetaData voice)
        {
            if ("Microsoft.Speech".ToLowerInvariant() != voice.Type.ToLowerInvariant())
            {
                return false;
            }
            return Synthesizer.GetInstalledVoices().Any(v =>
                v.Enabled && v.VoiceInfo.Name.ToLowerInvariant() == voice.Name.ToLowerInvariant());
        }
    }
}
