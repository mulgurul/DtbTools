using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Speech.AudioFormat;
using System.Speech.Synthesis;
using System.Xml.Linq;
using NAudio.Wave;

namespace DtbSynthesizerLibrary.Xml
{
    public class SystemSpeechXmlSynthesizer : IXmlSynthesizer
    {
        static SystemSpeechXmlSynthesizer()
        {
            Synthesizer = new SpeechSynthesizer();
            SynthesizerList = Synthesizer
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

        protected void AppendElementToPromptBuilder(XElement element, PromptBuilder promptBuilder, IDictionary<string, XText> bookmarks)
        {
            foreach (var node in element.Nodes())
            {
                if (node is XElement elem)
                {
                    AppendElementToPromptBuilder(elem, promptBuilder, bookmarks);
                }
                else if (node is XText text)
                {
                    var nameSuffix = bookmarks.Count.ToString("000000");
                    bookmarks.Add(nameSuffix, text);
                    promptBuilder.AppendBookmark($"B{nameSuffix}");
                    promptBuilder.AppendText(text.Value);
                    promptBuilder.AppendBookmark($"E{nameSuffix}");
                }
            }
        }




        public TimeSpan SynthesizeElement(XElement element, WaveFileWriter writer, string src = "")
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            var startOffset = writer.TotalTime;
            var audioStream = new MemoryStream();
            Synthesizer.SetOutputToAudioStream(
                audioStream,
                new SpeechAudioFormatInfo(
                    writer.WaveFormat.SampleRate,
                    (AudioBitsPerSample)writer.WaveFormat.BitsPerSample,
                    (AudioChannel)writer.WaveFormat.Channels));
            Synthesizer.SelectVoice(Voice.Name);
            Synthesizer.Speak(element.Value);
            Synthesizer.SetOutputToNull();
            audioStream.WriteTo(writer);
            writer.Flush();
            element.AddAnnotation(new SyncAnnotation()
            {
                ClipBegin = startOffset,
                ClipEnd = writer.TotalTime,
                Element = element,
                Src = src
            });
            return writer.TotalTime.Subtract(startOffset);
        }

        public VoiceMetaData VoiceInfo => new VoiceMetaData()
        {
            Name = Voice.Name,
            Culture = Voice.Culture,
            Gender = Voice.Gender.ToString(),
            AdditionalInfo = new ReadOnlyDictionary<string, string>(Synthesizer.Voice.AdditionalInfo),
            Type = "System.Speech/SAPI5"
        };
    }
}
