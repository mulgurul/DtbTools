using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Speech.AudioFormat;

namespace DtbSynthesizerLibrary
{
    public class SystemSpeechXmlSynthesizer : IXmlSynthesizer
    {
        public SpeechAudioFormatInfo AudioFormat { get; set; }
        public string WaveFile { get; set; }
        public TimeSpan Offset { get; }
        public TimeSpan SynthesizeElement(XElement element, Func<XElement, CultureInfo> languageSelector)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<VoiceMetaData> GetVoices()
        {
            throw new NotImplementedException();
        }

        public bool IsVoiceSupported(VoiceMetaData voice)
        {
            throw new NotImplementedException();
        }
    }
}
