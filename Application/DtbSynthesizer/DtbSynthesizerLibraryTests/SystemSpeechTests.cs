using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Speech.AudioFormat;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Speech.Synthesis;
using System.Text.RegularExpressions;
using NAudio.Wave;

namespace DtbSynthesizerLibraryTests
{
    [TestClass]
    //[Ignore]//Not really tests, just playing with the installed voices
    public class SystemSpeechTests
    {
        public TestContext TestContext { get; set; }

        private string GetAudioFilePath(string name)
        {
            var path = Path.Combine(TestContext.TestDir, name);
            TestContext.AddResultFile(path);
            return path;
        }

        [TestMethod]
        public void ListVoicesTest()
        {
            var synth = new SpeechSynthesizer();
            foreach (var voiceInfo in synth.GetInstalledVoices().Select(v => v.VoiceInfo))
            {
                Console.WriteLine($"Voice {voiceInfo.Name} (ID='{voiceInfo.Id}'): {voiceInfo.Culture}, {voiceInfo.Age}, {voiceInfo.Gender}, {voiceInfo.Description}");
            }
        }

        [TestMethod]
        public void SpeakTest()
        {
            var synth = new SpeechSynthesizer();
            try
            {
                var af = GetAudioFilePath("SystemSpeechSpeakTest.wav");
                synth.SetOutputToWaveFile(af, new SpeechAudioFormatInfo(22050, AudioBitsPerSample.Sixteen, AudioChannel.Mono));
                var testData = new[]
                {
                    new[] {"da", "Jeg hedder {0} og jeg snakker dansk. Oppe på loftet læser man ikke, til gengæld kan man åbne et vindue"},
                    new[] {"da-DK", "Jeg hedder {0} og jeg snakker dansk"},
                    new[] {"en", "My name is {0} and I speak english"},
                    new[] {"en-US", "My name is {0} and I speak american english"},
                };
                foreach (var pair in testData)
                {
                    var ci = new CultureInfo(pair[0]);
                    var voices = ci.IsNeutralCulture
                        ? synth.GetInstalledVoices().Where(v =>
                            v.VoiceInfo.Culture.TwoLetterISOLanguageName == ci.TwoLetterISOLanguageName)
                        : synth.GetInstalledVoices(ci);
                    foreach (var voice in voices)
                    {
                        synth.SelectVoice(voice.VoiceInfo.Name);
                        var name = voice.VoiceInfo.Name;
                        if (Regex.IsMatch(name, @"\(\w\w-\w\w,\s*(\w+)\)$"))
                        {
                            name = Regex.Replace(name, @"^.+\(\w\w-\w\w,\s*(\w+)\)$", "$1");
                        }
                        synth.Speak(String.Format(pair[1], name));
                    }
                }
                synth.SetOutputToNull();
                var wr = new WaveFileReader(af);
                Assert.AreEqual(22050, wr.WaveFormat.SampleRate);
                Assert.AreEqual(16, wr.WaveFormat.BitsPerSample);
                Assert.AreEqual(1, wr.WaveFormat.Channels);
            }
            finally
            {
                synth.Dispose();
            }
        }
    }
}
