using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using DtbSynthesizerLibrary;
using DtbSynthesizerLibrary.Xhtml;
using DtbSynthesizerLibrary.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NAudio.Wave;

namespace DtbSynthesizerLibraryTests.Xhtml
{
    [TestClass]
    [DeploymentItem(@".\TestFiles")]
    [DeploymentItem(@".\libmp3lame.32.dll")]
    [DeploymentItem(@".\libmp3lame.64.dll")]
    public class XhtmlSynthesizerTests
    {
        public TestContext TestContext { get; set; }

        private XhtmlSynthesizer GetXhtmlSynthesizer(string xhtmlFile, bool encodeMp3 = false)
        {
            xhtmlFile = Path.Combine(TestContext.DeploymentDirectory, xhtmlFile);
            TestContext.AddResultFile(xhtmlFile);
            var synthesizer = new XhtmlSynthesizer()
            {
                XhtmlDocument = XDocument.Load(xhtmlFile, LoadOptions.SetBaseUri | LoadOptions.SetLineInfo),
                EncodeMp3 = encodeMp3
            };
            if (synthesizer.Body.Elements().Select(Utils.SetMissingIds).Sum() > 0)
            {
                synthesizer.XhtmlDocument.Save(
                    new Uri(synthesizer.XhtmlDocument.BaseUri).LocalPath);
            }
            return synthesizer;
        }

        [DataRow(@"Simple\Simple.html", false)]
        [DataRow(@"Sectioned\Sectioned.html", false)]
        [DataRow(@"Tables\Tables.html", false)]
        [DataRow(@"Simple\Simple.html", true)]
        [DataRow(@"Sectioned\Sectioned.html", true)]
        [DataRow(@"Tables\Tables.html", true)]
        [DataRow(@"Pages\Pages.html", true)]
        [DataTestMethod]
        public void SynthesizeTest(string xhtmlFile, bool encodeMp3)
        {
            var synthesizer = GetXhtmlSynthesizer(xhtmlFile, encodeMp3);
            synthesizer.Synthesize();

            Assert.AreEqual(
                synthesizer
                    .Body
                    .Descendants()
                    .Count(elem => synthesizer.HeaderNames.Contains(elem.Name)),
                synthesizer.AudioFiles.Count(),
                "Expected one audio file per heading");
            Console.WriteLine(
                $"Xhtml file {Path.GetFileName(new Uri(synthesizer.XhtmlDocument.BaseUri).LocalPath)}");

            foreach (var elem in synthesizer.Body.Descendants()
                .Where(e => e.Attribute("id")?.Value != null))
            {
                var anno = elem.Annotation<SyncAnnotation>();
                if (anno != null)
                {
                    Console.WriteLine(
                        $"#{elem.Attribute("id")?.Value}={anno.Src}:{anno.ClipBegin}-{anno.ClipEnd}");
                }
            }
            foreach (var audioFile in synthesizer
                .Body
                .Descendants().SelectMany(e => e.Annotations<SyncAnnotation>().Select(a =>
                    new Uri(new Uri(e.BaseUri), a.Src).LocalPath)).Distinct())
            {
                if (encodeMp3)
                {
                    using (var wr = new Mp3FileReader(audioFile))
                    {
                        Assert.AreEqual(synthesizer.AudioWaveFormat.SampleRate, wr.WaveFormat.SampleRate, $"Audio file {audioFile} has unexpected sample rate");
                        Assert.AreEqual(synthesizer.AudioWaveFormat.BitsPerSample, wr.WaveFormat.BitsPerSample, $"Audio file {audioFile} has unexpected bits per sample");
                        Assert.AreEqual(synthesizer.AudioWaveFormat.Channels, wr.WaveFormat.Channels, $"Audio file {audioFile} has unexpected number of channels");
                    }
                }
                else
                {
                    using (var wr = new WaveFileReader(audioFile))
                    {
                        Assert.AreEqual(synthesizer.AudioWaveFormat.SampleRate, wr.WaveFormat.SampleRate, $"Audio file {audioFile} has unexpected sample rate");
                        Assert.AreEqual(synthesizer.AudioWaveFormat.BitsPerSample, wr.WaveFormat.BitsPerSample, $"Audio file {audioFile} has unexpected bits per sample");
                        Assert.AreEqual(synthesizer.AudioWaveFormat.Channels, wr.WaveFormat.Channels, $"Audio file {audioFile} has unexpected number of channels");
                    }

                }
                TestContext.AddResultFile(audioFile);
            }
        }
    }
}
