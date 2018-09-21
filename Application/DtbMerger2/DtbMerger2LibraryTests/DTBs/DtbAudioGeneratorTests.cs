using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DtbMerger2LibraryTests.DTBs
{
    [TestClass]
    public class DtbAudioGeneratorTests
    {
        [TestMethod]
        public void NarrateTextsTest()
        {
            var texts = new[]
            {
                "This is the first sentence",
                "This is another sentence, incidentally the second",
                "This is the third and final sentence"
            };
            var res = DtbAudioGenerator.NarrateTexts(texts, "texts.mp3").ToList();
            Assert.AreEqual(3, res.Count);
            Assert.IsTrue(File.Exists("texts.mp3"));
            var mp3File = new FileInfo("texts.mp3");
            Assert.IsTrue(mp3File.Length > 0);
        }
    }
}
