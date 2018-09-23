using System;
using System.IO;
using System.Linq;
using Google.Cloud.TextToSpeech.V1;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DtbSynthesizerLibraryTests
{
    [TestClass]
    [DeploymentItem("grpc_csharp_ext.x86.dll")]
    [DeploymentItem("grpc_csharp_ext.x64.dll")]
    public class GoogleCloudTextToSpeechTests
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
            var client = TextToSpeechClient.Create();
            foreach (var voice in client.ListVoices("").Voices.OrderBy(v => v.LanguageCodes.FirstOrDefault()).ThenBy(v => v.Name))
            {
                Console.WriteLine($"Voice {voice.Name}, Languages {voice.LanguageCodes.Aggregate((s,v)=>$"{s};{v}")}), Gender {voice.SsmlGender}, Natural Sample Rate {voice.NaturalSampleRateHertz}");
            }
        }


        [TestMethod]
        public void SpeakTest()
        {
            var client = TextToSpeechClient.Create();
            var voiceResponse = client.ListVoices(new ListVoicesRequest
            {
                LanguageCode = ""
            });

            var langCodes = voiceResponse.Voices.SelectMany(v => v.LanguageCodes).ToList();



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


            // Select the type of audio file you want returned.
            var config = new AudioConfig
            {
                AudioEncoding = AudioEncoding.Linear16,
                SampleRateHertz = 22050
            };

            foreach (var data in testData.Where(d => langCodes.Contains(d[0])))
            {
                var input = new SynthesisInput {Text = String.Format(data[1], "google")};
                var voice = new VoiceSelectionParams
                {
                    LanguageCode = data[0],
                    SsmlGender = SsmlVoiceGender.Neutral
                };
                var response = client.SynthesizeSpeech(new SynthesizeSpeechRequest
                {
                    Input = input,
                    Voice = voice,
                    AudioConfig = config
                });
                using (Stream output = File.Create(GetAudioFilePath($"Google_{data[0]}.wav")))
                {
                    response.AudioContent.WriteTo(output);
                }
            }
        }
    }
}
