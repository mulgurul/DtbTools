using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Amazon.Polly;
using Amazon.Polly.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NAudio.Wave;

namespace DtbSynthesizerLibraryTests
{
    [TestClass]
    public class AmazonPollyTests
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
            var client = new AmazonPollyClient();
            foreach (var voice in client.DescribeVoices(new DescribeVoicesRequest()).Voices.OrderBy(v => v.LanguageCode.Value))
            {
                Console.WriteLine($"Voice {voice.Name}, Language {voice.LanguageCode}, Gender {voice.Gender}, Id {voice.Id}");
            }
        }

        [TestMethod]
        public void SpeakTest()
        {
            var client = new AmazonPollyClient();


            var langCodes = client.DescribeVoices(new DescribeVoicesRequest()).Voices.Select(v => v.LanguageCode.Value);

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

            foreach (var data in testData.Where(d => langCodes.Contains(d[0])))
            {
                foreach (var voice in client.DescribeVoices(new DescribeVoicesRequest() {LanguageCode = data[0]}).Voices)
                {
                    var response = client.SynthesizeSpeech(new SynthesizeSpeechRequest()
                    {
                        Text = String.Format(data[1], voice.Name),
                        VoiceId = voice.Id,
                        TextType = TextType.Text,
                        OutputFormat = OutputFormat.Pcm,
                        SampleRate = "16000"
                    });
                    using (var writer = new WaveFileWriter(
                        GetAudioFilePath($"AmazonPolly_{data[0]}_{voice.Name}.wav"),
                        new WaveFormat(16000, 16, 1)))
                    {
                        var buf = new byte[1024];
                        int count;
                        while ((count = response.AudioStream.Read(buf, 0, buf.Length)) > 0)
                        {
                            writer.Write(buf, 0, count);
                        }

                    }
                }

            }
        }
    }
    }
