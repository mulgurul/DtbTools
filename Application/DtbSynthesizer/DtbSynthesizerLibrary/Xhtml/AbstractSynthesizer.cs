using System;
using System.Globalization;
using DtbSynthesizerLibrary.Xml;
using NAudio.Wave;

namespace DtbSynthesizerLibrary.Xhtml
{
    public abstract class AbstractSynthesizer
    {
        public Func<CultureInfo, IXmlSynthesizer> SynthesizerSelector { get; set; } 
            = ci => Utils.GetPrefferedXmlSynthesizerForCulture(ci);

        public IXmlSynthesizer DefaultSynthesizer { get; set; }
        public int Mp3BitRate { get; set; } = 32;
        public WaveFormat AudioWaveFormat { get; set; } = new WaveFormat(22050, 1);
        public string AudioFilePrefix { get; set; } = $"aud{new Random().Next(Char.MaxValue):x4}";
        public abstract bool Synthesize();
    }
}