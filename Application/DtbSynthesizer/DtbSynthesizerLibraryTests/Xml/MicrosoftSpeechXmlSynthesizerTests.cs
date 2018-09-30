using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using DtbSynthesizerLibrary;
using DtbSynthesizerLibrary.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NAudio.Wave;

namespace DtbSynthesizerLibraryTests.Xml
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

        [DataRow(
            @"
<html lang='en-US'>
    <body>
        <table>
            <caption>A simple little table</caption>
            <tr><th>Column A</th><th>Column B</th><th>Column C</th></tr>
            <tr><td>Cell A1</td><td>Cell B1</td><td>Cell C1</td></tr>
            <tr><td>Cell A2</td><td>Cell B2</td><td>Cell C2</td></tr>
            <tr><td>Cell A3</td><td>Cell B3</td><td>Cell C3</td></tr>
        </table>
    </body>
</html>
", 
            0)]
        [DataRow(
            @"
<html lang='en-US'>
    <body>
        <p>This is a single paragraph. It contains an <em>emphasized</em> word</p>
        <p lang='da-DK'>I midten en sætning på dansk</p>
        <p>This is a third paragraph</p>
    </body>
</html>",
            1)]
        [DataTestMethod]
        public void SynthesizeElementTest(string xml, int no)
        {
            var doc = XDocument.Parse(xml);

            var body = doc.Elements("html").Elements("body").First();
            var waveFile = GetAudioFilePath($"SystemSpeech{no:D2}.wav");
            var writer = new WaveFileWriter(waveFile, new WaveFormat(22050, 1));
            try
            {
                var dur = TimeSpan.Zero;
                foreach (var elem in body.Elements())
                {
                    var ci = Utils.SelectCulture(elem);
                    var synth = SystemSpeechXmlSynthesizer.GetPreferedVoiceForCulture(ci);
                    dur += synth.SynthesizeElement(elem, writer);
                }
                var prevClipEnd = TimeSpan.Zero;
                foreach (var text in body.DescendantNodes().OfType<XText>())
                {
                    var anno = text.Annotation<SyncAnnotation>();
                    Assert.IsNotNull(
                        anno,
                        $"Text node {text} has no SyncAnnotation");
                    Assert.AreEqual(prevClipEnd, anno.ClipBegin);
                    prevClipEnd = anno.ClipEnd;
                    Assert.IsTrue(anno.ClipBegin < anno.ClipEnd, $"Text node {text} SyncAnnotation ClipEnd is not before ClipBEgin");
                }
                Assert.AreEqual(dur, prevClipEnd, "Last ClipEnd is not equal to dur");
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
