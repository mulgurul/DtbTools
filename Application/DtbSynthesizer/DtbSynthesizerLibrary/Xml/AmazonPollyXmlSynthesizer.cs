using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Amazon.Polly;
using Amazon.Polly.Model;
using NAudio.Wave;

namespace DtbSynthesizerLibrary.Xml
{
    /// <summary>
    /// An implementation of <see cref="IXmlSynthesizer"/> using Amazon Polly 
    /// </summary>
    /// <remarks>
    /// Please remark, that a you need cretentials, that among other places can be stored in the file ~\.aws\cretendials.
    /// Refer to AWS SDK for.Net documentation for more information, 
    /// e.g. https://docs.aws.amazon.com/sdk-for-net/v3/developer-guide/net-dg-config-creds.html 
    /// </remarks>
    public class AmazonPollyXmlSynthesizer : IXmlSynthesizer
    {
        private static List<AmazonPollyXmlSynthesizer> synthesizerList;

        private static readonly AmazonPollyClient Client = new AmazonPollyClient();

        /// <summary>
        /// Get all <see cref="IXmlSynthesizer"/>s provided by Amazon Polly, that is one for each Google Cloud Voice
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
                            .DescribeVoices(new DescribeVoicesRequest())
                            .Voices
                            .Select(v => new AmazonPollyXmlSynthesizer(v))
                            .OrderBy(v => v.Voice.LanguageCode.Value)
                            .ToList();
                    }
                }
                return (synthesizerList??new List<AmazonPollyXmlSynthesizer>()).AsReadOnly();
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

        protected Voice Voice { get; }

        private AmazonPollyXmlSynthesizer(Voice voice)
        {
            Voice = voice;
        }


        /// <inheritdoc />
        public TimeSpan SynthesizeElement(XElement element, WaveFileWriter writer, string src = "")
        {
            if (element.DescendantNodes().OfType<XText>().All(t => Utils.GetWhiteSpaceNormalizedLength(t) == 0))
            {
                return TimeSpan.Zero;
            }
            var startOffset = writer.TotalTime;
            if (writer.WaveFormat.BitsPerSample != 16)
            {
                throw new ApplicationException($"Unsupported bits per sample {writer.WaveFormat.BitsPerSample} for Amazon Polly: only 16 bit is supported");
            }
            if (writer.WaveFormat.Channels != 1)
            {
                throw new ApplicationException($"Unsupported number of channels {writer.WaveFormat.Channels} for Amazon Polly: only mono is supported");
            }
            var response = Client.SynthesizeSpeech(
                new SynthesizeSpeechRequest()
                {
                    VoiceId = Voice.Id,
                    LanguageCode = Voice.LanguageCode,
                    Text = element.Value,
                    OutputFormat = OutputFormat.Pcm,
                    SampleRate = writer.WaveFormat.SampleRate.ToString()
                });
            var buf = new byte[1024];
            int count;
            while ((count = response.AudioStream.Read(buf, 0, buf.Length)) > 0)
            {
                writer.Write(buf, 0, count);
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
        public VoiceMetaData VoiceInfo => new VoiceMetaData("Amazon.Polly", Voice.Name)
        {
            Culture = new CultureInfo(Voice.LanguageCode.Value),
            Gender = Voice.Gender.ToString()
        };
    }
}
