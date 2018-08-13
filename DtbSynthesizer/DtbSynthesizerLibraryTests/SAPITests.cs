using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SpeechLib;

namespace DtbSynthesizerLibraryTests
{
    [TestClass]
    public class SAPITests
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
            var voice = new SpVoice();
            foreach (SpObjectToken token in voice.GetVoices())
            {

                Console.WriteLine($"{token.GetDescription()} - language {token.GetAttribute("Language")} - id {token.Id}");
            }
        }

        private TimeSpan GetOffset(ISpeechBaseStream stream)
        {
            double bytesPerSecond = stream.Format.GetWaveFormatEx().AvgBytesPerSec;
            return TimeSpan.FromSeconds(
                Convert.ToInt64(stream.Seek(0, SpeechStreamSeekPositionType.SSSPTRelativeToCurrentPosition)) / bytesPerSecond);
        }

        [TestMethod]
        public void SpeakTest()
        {
            var voice = new SpVoice();
            var wavFileStream = new SpFileStream();
            wavFileStream.Open(GetAudioFilePath("SAPISpeakTest.wav"), SpeechStreamFileMode.SSFMCreateForWrite);
            double bytesPerSecond = wavFileStream.Format.GetWaveFormatEx().AvgBytesPerSec;
            voice.AudioOutputStream = wavFileStream;
            foreach (SpObjectToken token in voice.GetVoices())
            {
                voice.Voice = token;
                string text;
                switch (voice.Voice.GetAttribute("Language"))
                {
                    case "406":
                        text = $"Jeg er SAPI stemmen {token.GetDescription()}";
                        break;
                    default:
                        text = $"I am the SAPI voice {token.GetDescription()}";
                        break;
                }
                Console.WriteLine($"Speaking text {text} - offset before {GetOffset(wavFileStream)}");
                voice.Speak(text);
                Console.WriteLine($"Spoke text {text} - position after {GetOffset(wavFileStream)}");
            }
            wavFileStream.Close();
        }
    }
}
