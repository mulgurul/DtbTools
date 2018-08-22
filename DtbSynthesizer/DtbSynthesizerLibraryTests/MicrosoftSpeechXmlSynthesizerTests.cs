using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using DtbSynthesizerLibrary;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NAudio.Wave;

namespace DtbSynthesizerLibraryTests
{
    [TestClass]
    public class MicrosoftSpeechXmlSynthesizerTests
    {
        public TestContext TestContext { get; set; }

        private string GetAudioFilePath(string name)
        {
            var path = Path.Combine(TestContext.TestDir, name);
            TestContext.AddResultFile(path);
            return path;
        }

        [TestMethod]
        public void SynthesizeElementTest()
        {
            var doc = XDocument.Parse(
                "<html lang='en-US'><body><p>This is a single paragraph. It contains an <em>emphasized</em> word</p><p lang='da-DK'>I midten en sætning på dansk</p><p>This is a third paragraph</p></body></html>");

            var body = doc.Elements("html").Elements("body").First();
            var waveFile = GetAudioFilePath("MicrosoftSpeech.wav");
            var writer = new WaveFileWriter(waveFile, new WaveFormat(22050, 1));
            try
            {
                var dur = TimeSpan.Zero;
                foreach (var elem in body.Elements())
                {
                    var ci = Utils.SelectCulture(elem);
                    var synth = MicrosoftSpeechXmlSynthesizer.GetPreferedVoiceForCulture(ci);
                    dur += synth.SynthesizeElement(elem, writer);
                }
                foreach (var text in body.DescendantNodes().OfType<XText>())
                {
                    Assert.IsTrue(
                        text.Annotations<SyncAnnotation>().Count() == 1 || String.IsNullOrWhiteSpace(text.Value),
                        $"Text node {text} has no SyncAnnotation");
                }
                var annotations = body.DescendantNodes().SelectMany(n => n.Annotations<SyncAnnotation>());
                var sum = TimeSpan.FromMilliseconds(
                    annotations.Select(a => a?.ClipEnd.Subtract(a.ClipBegin).TotalMilliseconds).Sum() ?? 0);
                Assert.AreEqual(
                    dur,
                    sum,
                    $"Expected sum of body element durations to be {dur}");
            }
            finally
            {
                writer.Close();
            }
        }

    }
}
