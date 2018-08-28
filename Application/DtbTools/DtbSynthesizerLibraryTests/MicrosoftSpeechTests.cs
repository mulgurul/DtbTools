using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Speech.AudioFormat;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Speech.Synthesis;
using NAudio.Wave;

namespace DtbSynthesizerLibraryTests
{
    [TestClass]
    //[Ignore]//Not really tests, just playing with the installed voices
    public class MicrosoftSpeechTests
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
                synth.SetOutputToWaveFile(GetAudioFilePath("MicrosoftSpeechSpeakTest.wav"));
                var testData = new[]
                {
                    new[] {"da", "Jeg hedder {0} og jeg snakker dansk"},
                    new[] {"da-DK", "Jeg hedder {0} og jeg snakker dansk"},
                    new[] {"en", "My name is {0} and I speak english"},
                    new[] {"en-US", "My name is {0} and I speak american english"},
                    new[] {"en-GB", "My name is {0} and I speak british english"},
                    new[] {"en-CA", "My name is {0} and I speak canadian english"},
                    new[] {"en-AU", "My name is {0} and I speak australian english"},
                    new[] {"en-IN", "My name is {0} and I speak indian english"},
                    new[] {"sv-SE", "Jag heter {0} och jag talar svenska"},
                    new[] {"nb-NO", "Mitt navn er {0} og jeg snakker norsk"},

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

            }
            finally
            {
                synth.Dispose();
            }
        }

        [TestMethod]
        public void CreateEmptyWaveFileTest()
        {
            var path = GetAudioFilePath("Empty.wav");
            //var synth = new SpeechSynthesizer();
            //var safi = new SpeechAudioFormatInfo(22050, AudioBitsPerSample.Sixteen, AudioChannel.Mono);
            //synth.SetOutputToWaveFile(path, safi);
            //synth.Speak("");
            //synth.SetOutputToNull();
            var safi = new WaveFormat(22050, 16, 1);
            var writer = new WaveFileWriter(path, safi);
            writer.Close();

            Assert.IsTrue(File.Exists(path), $"Empty wave file {path} was not created");
            var reader = new WaveFileReader(path);
            Assert.AreEqual(0, reader.SampleCount, "Expected empty wavefile to have no samples");
            Assert.AreEqual(TimeSpan.Zero, reader.TotalTime, "Expected total time to be zero");
            Assert.AreEqual(safi.SampleRate, reader.WaveFormat.SampleRate, $"Expected sample rate to be {safi.SampleRate}");
            Assert.AreEqual(safi.BitsPerSample, reader.WaveFormat.BitsPerSample, $"Expected bits per samples to be {safi.BitsPerSample}");
            Assert.AreEqual(safi.Channels, reader.WaveFormat.Channels, $"Expected number of channels to be {safi.Channels}");
        }
    }
}
