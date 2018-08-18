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
    public class SystemSpeechXmlSynthesizerTests
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
            var waveFile = GetAudioFilePath("SystemSpeech.wav");
            var writer = new WaveFileWriter(waveFile, new WaveFormat(22050, 1));
            try
            {
                var dur = 0.0;
                foreach (var elem in body.Elements())
                {
                    var ci = Utils.SelectCulture(elem);
                    var synth = SystemSpeechXmlSynthesizer.GetPreferedVoiceForCulture(ci);
                    dur += synth.SynthesizeElement(elem, writer).TotalMilliseconds;
                }
                Assert.IsTrue(body.Descendants().All(e => e.Annotation<SyncAnnotation>()!=null));
                var annotations = body.Elements().Select(e => e.Annotation<SyncAnnotation>()).ToList();
                var sum = annotations
                              .Select(a => a?.ClipEnd.Subtract(a.ClipBegin).TotalMilliseconds).Sum()??0;
                Assert.AreEqual(
                    dur,
                    sum,
                    $"Expected sum of body element durations to be {dur}");
                var lastEnd = TimeSpan.Zero;
                foreach (var p in body.Elements("p"))
                {
                    var anno = p.Annotation<SyncAnnotation>();
                    Assert.IsNotNull(anno);
                    Assert.AreEqual(lastEnd, anno.ClipBegin);
                    lastEnd = anno.ClipEnd;
                }

            }
            finally
            {
                writer.Close();
            }
        }

    }
}
