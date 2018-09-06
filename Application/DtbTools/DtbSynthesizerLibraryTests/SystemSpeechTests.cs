using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Speech.Synthesis;
using System.Text.RegularExpressions;

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
                synth.SetOutputToWaveFile(GetAudioFilePath("SystemSpeechSpeakTest.wav"));
                var testData = new[]
                {
                    new[] {"da", "Jeg hedder {0} og jeg snakker dansk"},
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

            }
            finally
            {
                synth.Dispose();
            }
        }
    }
}
