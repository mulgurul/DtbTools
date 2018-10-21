using System;
using System.Globalization;
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

        [DataRow(@"Pages\Pages.html", 0, 5, 5, 0)]
        [DataTestMethod]
        public void GenerateDtbWithPagesTest(string xhtmlFile, int pageFront, int pageNormal, int maxPageNormal, int pageSpecial)
        {
            var synthesizer = GetXhtmlSynthesizer(xhtmlFile, true);
            synthesizer.Synthesize();
            synthesizer.GenerateDaisy202SmilFiles();
            Assert.AreEqual(synthesizer.AudioFiles.Count(), synthesizer.SmilFiles.Count, $"Expected one smil file per audio file");
            synthesizer.GenerateNccDocument();
            Assert.AreEqual(pageFront.ToString(), Utils.GetMetaContent(synthesizer.NccDocument, "ncc:pageFront"), "Unexpected ncc:pageFront");
            Assert.AreEqual(pageNormal.ToString(), Utils.GetMetaContent(synthesizer.NccDocument, "ncc:pageNormal"), "Unexpected ncc:pageNormal");
            Assert.AreEqual(maxPageNormal.ToString(), Utils.GetMetaContent(synthesizer.NccDocument, "ncc:maxPageNormal"), "Unexpected ncc:maxPageNormal");
            Assert.AreEqual(pageSpecial.ToString(), Utils.GetMetaContent(synthesizer.NccDocument, "ncc:pageSpecial"), "Unexpected ncc:pageSpecial");
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

        /// <summary>
        /// Parses a Daisy 2.02 SMIL 1.0 file clip attribute value
        /// </summary>
        /// <param name="val">The value</param>
        /// <returns>The <see cref="TimeSpan"/> equivalent of the value</returns>
        public static TimeSpan ParseSmilClip(string val)
        {
            if (val == null) throw new ArgumentNullException(nameof(val));
            if (String.IsNullOrWhiteSpace(val))
            {
                throw new ArgumentException($"Value is empty", nameof(val));
            }
            val = val.Trim();
            if (val.StartsWith("npt=") && val.EndsWith("s"))
            {
                var secs = Double.Parse(
                    val.Substring(4, val.Length - 5),
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture);
                return TimeSpan.FromSeconds(secs);
            }
            else
            {
                throw new ArgumentException($"Value {val} is not a valid Daisy 2.02 smil clip value", nameof(val));
            }
        }

        [DataRow(@"Simple\Simple.html", false)]
        [DataRow(@"Sectioned\Sectioned.html", false)]
        [DataRow(@"Tables\Tables.html", false)]
        [DataRow(@"Simple\Simple.html", true)]
        [DataRow(@"Sectioned\Sectioned.html", true)]
        [DataRow(@"Tables\Tables.html", true)]
        [DataRow(@"Pages\Pages.html", true)]
        [DataTestMethod]
        public void GenerateDaisy202SmilFilesAndNccDocumentTest(string xhtmlFile, bool encodeMp3)
        {
            var synthesizer = GetXhtmlSynthesizer(xhtmlFile, encodeMp3);
            synthesizer.Synthesize();
            synthesizer.GenerateDaisy202SmilFiles();
            Assert.AreEqual(synthesizer.AudioFiles.Count(), synthesizer.SmilFiles.Count, $"Expected one smil file per audio file");
            synthesizer.GenerateNccDocument();
            Assert.IsNotNull(synthesizer.NccDocument, "No ncc document was generated");
            Assert.AreEqual(
                synthesizer.SmilFiles.Count(),
                synthesizer
                    .NccDocument
                    .Root
                    ?.Element(XhtmlSynthesizer.XhtmlNs+"body")
                    ?.Elements()
                    .Count(e => 
                        Enumerable.Range(1, 6).Select(i => XhtmlSynthesizer.XhtmlNs+ $"h{i}").Contains(e.Name)),
                "Expected one ncc heading per smil file");
            Assert.AreEqual(
                "Daisy 2.02",
                Utils.GetMetaContent(synthesizer.NccDocument, "dc:format"),
                "Expected dc:format=Daisy 2.02 meta");
            Assert.AreEqual(
                (2+synthesizer.AudioFiles.Count()+synthesizer.SmilFiles.Count).ToString(),
                Utils.GetMetaContent(synthesizer.NccDocument, "ncc:files"),
                "Expected dc:format=Daisy 2.02 meta");
            var totalElapsedTime = TimeSpan.Zero;
            foreach (var smil in synthesizer.SmilFiles)
            {
                foreach (var audio in smil
                    .Value
                    .Descendants("body").Single()
                    .Elements("seq").Single()
                    .Descendants("audio"))
                {
                    Assert.IsTrue(
                        ParseSmilClip(audio.Attribute("clip-begin")?.Value) < ParseSmilClip(audio.Attribute("clip-end")?.Value),
                        $"clip-begin must be before clip-end (audio[@id='{audio.Attribute("id")?.Value??""}'])");
                }
                var calDur = TimeSpan.FromSeconds(
                    smil
                        .Value
                        .Descendants("body").Single()
                        .Elements("seq").Single()
                        .Descendants("audio")
                        .Select(audio =>
                            ParseSmilClip(audio.Attribute("clip-end")?.Value) -
                            ParseSmilClip(audio.Attribute("clip-begin")?.Value))
                        .Sum(ts => ts.TotalSeconds));
                var durAttr = smil.Value.Descendants("body").Single().Elements("seq").Single().Attribute("dur");
                var dur = TimeSpan.FromSeconds(double.Parse(
                    (durAttr?.Value ?? "").TrimEnd('s'),
                    CultureInfo.InvariantCulture));
                Assert.AreEqual(calDur, dur, "dur attribute on main seq is wrong");
                Assert.IsTrue(
                    dur > TimeSpan.Zero,
                    $"Unexpected duration {dur} in smil {smil.Key}");
                Assert.AreEqual(
                    Utils.GetHHMMSSFromTimeSpan(dur),
                    Utils.GetMetaContent(smil.Value, "ncc:timeInThisSmil"),
                    $"Unexpected ncc:timeInThisSmil meta in smil {smil.Key}");
                Assert.AreEqual(
                    Utils.GetHHMMSSFromTimeSpan(totalElapsedTime),
                    Utils.GetMetaContent(smil.Value, "ncc:totalElapsedTime"),
                    $"Unexpected ncc:totalElapsedTime meta in smil {smil.Key}");
                totalElapsedTime += dur;
            }
        }
    }
}
