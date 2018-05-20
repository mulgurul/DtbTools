using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Speech.AudioFormat;
using System.Speech.Synthesis;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using DtbMerger2Library.Daisy202;
using NAudio.Lame;
using NAudio.Wave;

namespace DtbMerger2LibraryTests.DTBs
{
    public static class DtbAudioGenerator
    {
        public static SpeechAudioFormatInfo SpeechFormat =>
            new SpeechAudioFormatInfo(22050, AudioBitsPerSample.Sixteen, AudioChannel.Mono);

        public static WaveFormat WaveFormat => new WaveFormat(SpeechFormat.SamplesPerSecond, SpeechFormat.BitsPerSample, SpeechFormat.ChannelCount);

        public static TimeSpan NarrateText(string text, Stream outputStream)
        {
            var narrator = new SpeechSynthesizer();
            narrator.SetOutputToAudioStream(outputStream, SpeechFormat);
            long before = outputStream.Position;
            narrator.Speak(text);
            return TimeSpan.FromSeconds((double)(outputStream.Position-before) / SpeechFormat.AverageBytesPerSecond);
        }

        public static void SaveAsMp3(Stream audioStream, string mp3Path)
        {
            using (var rawSource = new RawSourceWaveStream(audioStream, WaveFormat))
            {
                using (var mp3Writer = new LameMP3FileWriter(mp3Path, WaveFormat, LAMEPreset.ABR_128, null))
                {
                    rawSource.CopyTo(mp3Writer);
                }
            }
        }

        public static IEnumerable<TimeSpan> NarrateTexts(IEnumerable<string> texts, string mp3Path)
        {
            using (var ms = new MemoryStream())
            {
                var res = texts.Select(t => NarrateText(t, ms)).ToList();
                ms.Position = 0;
                SaveAsMp3(ms, mp3Path);
                return res;
            }
        }

        public static void NarrateTextsForSmilFile(XDocument smilDocument)
        {
            var smilPars = smilDocument.Descendants("par").ToList();
            var audioFileName = smilDocument
                .Descendants("audio")
                .Select(audio => audio.Attribute("src"))
                .FirstOrDefault(src => src != null)
                ?.Value.Split('#').FirstOrDefault()??"aud.mp3";
            var texts = smilPars
                .Select(par => Utils.GetUri(par.Element("text")?.Attribute("src")))
                .Select(uri =>
                    XDocument.Load(Uri.UnescapeDataString(uri.AbsolutePath)).Descendants()
                        .FirstOrDefault(e => e.Attribute("id")?.Value == uri.Fragment.TrimStart('#'))?.Value ?? "");
            var durs = NarrateTexts(texts, Uri.UnescapeDataString(new Uri(new Uri(smilDocument.BaseUri), audioFileName).AbsolutePath)).ToList();
            var elapsed = TimeSpan.Zero;
            for (int i = 0; i < smilPars.Count; i++)
            {
                var audio = smilPars[i].Descendants("audio").FirstOrDefault();
                audio?.SetAttributeValue("clip-begin", $"npt={elapsed.TotalSeconds.ToString("F3", CultureInfo.InvariantCulture)}s");
                elapsed += durs[i];
                audio?.SetAttributeValue("clip-end", $"npt={elapsed.TotalSeconds.ToString("F3", CultureInfo.InvariantCulture)}s");
                foreach (var followingAudio in audio?.ElementsAfterSelf("audio") ?? new List<XElement>())
                {
                    followingAudio.Remove();
                }
            }
        }
    }
}
