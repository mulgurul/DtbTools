using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Google.Cloud.TextToSpeech.V1;
using NAudio.Wave;

namespace DtbSynthesizerLibrary.Xml
{
    /// <summary>
    /// An implementation of <see cref="IXmlSynthesizer"/> using Google Cloud TextToSpeech V1 
    /// </summary>
    /// <remarks>
    /// Please remark, that a you need a json service account key file 
    /// and you need environment variable GOOGLE_APPLICATION_CREDENTIALS to point to this json file.
    /// Refer to Google Cloud TextToSpeech V1 documentation for more information, 
    /// e.g. https://cloud.google.com/text-to-speech/docs/quickstart-protocol 
    /// </remarks>
    public class GoogleCloudXmlSynthesizer : IXmlSynthesizer
    {
        private static List<GoogleCloudXmlSynthesizer> synthesizerList;

        private static readonly TextToSpeechClient Client = TextToSpeechClient.Create();

        /// <summary>
        /// Get all <see cref="IXmlSynthesizer"/>s provided by Google Cloud, that is one for each Google Cloud voice
        /// </summary>
        public static IReadOnlyCollection<IXmlSynthesizer> Synthesizers
        {
            get
            {
                if (synthesizerList == null)
                {
                    if (File.Exists(Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS")))
                    {
                        synthesizerList = Client
                            .ListVoices("")
                            .Voices
                            .Select(v => new GoogleCloudXmlSynthesizer(v))
                            .OrderBy(v => v.voice.LanguageCodes.FirstOrDefault())
                            .ThenBy(v => v.voice.Name)
                            .ToList();
                    }
                }
                return (synthesizerList??new List<GoogleCloudXmlSynthesizer>()).AsReadOnly();
            }
        }

        /// <summary>
        /// Gets the prefered Google Cloud <see cref="IXmlSynthesizer"/> for a given culture
        /// </summary>
        /// <param name="ci">The culture</param>
        /// <returns>The prefered Google Cloud  <see cref="IXmlSynthesizer"/> for the given culture</returns>
        public static IXmlSynthesizer GetPreferedVoiceForCulture(CultureInfo ci)
        {
            return Utils.GetPrefferedXmlSynthesizerForCulture(ci, Synthesizers);
        }

        private readonly Voice voice;

        private GoogleCloudXmlSynthesizer(Voice voice)
        {
            this.voice = voice;
        }


        /// <inheritdoc />
        public TimeSpan SynthesizeElement(XElement element, WaveFileWriter writer, string src = "")
        {
            if (element.DescendantNodes().OfType<XText>().All(t => Utils.GetWhiteSpaceNormalizedLength(t) == 0))
            {
                return TimeSpan.Zero;
            }
            var startOffset = writer.TotalTime;
            var resp = Client.SynthesizeSpeech(
                new SynthesisInput() {Text = element.Value},
                new VoiceSelectionParams()
                {
                    Name = voice.Name,
                    LanguageCode = voice.LanguageCodes.First(),
                    SsmlGender = voice.SsmlGender
                },
                new AudioConfig()
                {
                    AudioEncoding = AudioEncoding.Linear16,
                    SampleRateHertz = writer.WaveFormat.SampleRate
                });
            var memStream = new MemoryStream(resp.AudioContent.ToByteArray());
            var reader = new WaveFileReader(memStream);
            if (reader.WaveFormat.Channels != writer.WaveFormat.Channels)
            {
                throw new ApplicationException(
                    $"Unsupported number of channels for Google.Cloud: {writer.WaveFormat.Channels} - must be {reader.WaveFormat.Channels}");
            }
            if (reader.WaveFormat.BitsPerSample != writer.WaveFormat.BitsPerSample)
            {
                throw new ApplicationException(
                    $"Unsupported bits per sample for Google.Cloud: {writer.WaveFormat.BitsPerSample} - must be {reader.WaveFormat.BitsPerSample}");
            }
            if (reader.WaveFormat.SampleRate != writer.WaveFormat.SampleRate)
            {
                throw new ApplicationException(
                    $"Unsupported sample rate for Google.Cloud: {writer.WaveFormat.SampleRate} - must be {reader.WaveFormat.SampleRate}");
            }
            reader.CurrentTime = TimeSpan.Zero;
            var buf = new byte[1024];
            while (true)
            {
                int count = reader.Read(buf, 0, buf.Length);
                writer.Write(buf, 0, count);
                if (count < buf.Length)
                {
                    break;
                }
            }
            var textNodes = element.DescendantNodes().OfType<XText>().ToList();
            var secsPerChar = writer.TotalTime.Subtract(startOffset).TotalSeconds / textNodes.Select(Utils.GetWhiteSpaceNormalizedLength).Sum();
            var offset = startOffset;
            foreach (var t in textNodes)
            {
                var anno = new SyncAnnotation()
                {
                    ClipBegin = offset,
                    ClipEnd = offset.Add(TimeSpan.FromSeconds(secsPerChar * Utils.GetWhiteSpaceNormalizedLength(t))),
                    Element = t.Parent,
                    Text = t,
                    Src = src
                };
                offset = anno.ClipEnd;
                t.AddAnnotation(anno);
            }
            // ReSharper disable once PossibleNullReferenceException
            textNodes.Last().Annotation<SyncAnnotation>().ClipEnd = writer.TotalTime;
            return writer.TotalTime.Subtract(startOffset);
        }

        /// <inheritdoc />
        public VoiceMetaData VoiceInfo => new VoiceMetaData()
        {
            Culture = voice.LanguageCodes.Select(lc => new CultureInfo(lc)).FirstOrDefault(),
            Name = voice.Name,
            Gender = voice.SsmlGender.ToString(),
            Type = "Google.Cloud"
        };
    }
}
