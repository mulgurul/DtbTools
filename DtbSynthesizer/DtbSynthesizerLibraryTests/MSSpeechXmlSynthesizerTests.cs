using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using DtbSynthesizerLibrary;
using Microsoft.Speech.AudioFormat;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DtbSynthesizerLibraryTests
{
    [TestClass]
    public class MSSpeechXmlSynthesizerTests
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
                "<html><body><p>This is a single paragraph. It contains an <em>emphasized</em> word</p><p lang='da-DK'>I midten en sætning på dansk</p><p>This is a third paragraph</p></body></html>");
            var synth = new MSSpeechXmlSynthesizer
            {
                AudioFormat = new SpeechAudioFormatInfo(22050, AudioBitsPerSample.Sixteen, AudioChannel.Mono),
                WaveFile = GetAudioFilePath("mshtml.wav")
            };
            var body = doc.Elements("html").Elements("body").First();
            var langSel = new Func<XElement, CultureInfo>(element => element
                .Attributes()
                .Where(a => a.Name == "lang" || a.Name == XNamespace.Xml + "lang")
                .Select(a => new CultureInfo(a.Value))
                .FirstOrDefault() ?? new CultureInfo("en-US")
            );
            var dur = 0.0;
            foreach (var elem in body.Elements())
            {
                dur += synth.SynthesizeElement(elem, langSel).TotalMilliseconds;
            }
            Assert.IsTrue(body.Descendants().All(e => e.Annotation<SyncAnnotation>()!=null));
            var sum = body.Elements().Select(e => e.Annotation<SyncAnnotation>())
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
    }
}
