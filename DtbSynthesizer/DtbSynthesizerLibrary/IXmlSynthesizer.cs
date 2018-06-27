using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.Speech.AudioFormat;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace DtbSynthesizerLibrary
{
    /// <summary>
    /// Interface for a speech synthesizer, that can generate synthetic speech for <see cref="XElement"/>s
    /// </summary>
    public interface IXmlSynthesizer
    {
        /// <summary>
        /// The <see cref="SpeechAudioFormatInfo"/> used by the synthesizer
        /// </summary>
        SpeechAudioFormatInfo AudioFormat { get; set; }

        /// <summary>
        /// Gets or sets the WaveFile the synthesized speech is written to
        /// </summary>
        string WaveFile { get; set; }

        /// <summary>
        /// Gets the offset of the synthesized speech audio within the current <see cref="WaveFile"/>
        /// </summary>
        TimeSpan Offset { get; }

        /// <summary>
        /// Generates synthetic speech for an <see cref="XElement"/> and all it's decendants, 
        /// adding <see cref="SyncAnnotation"/> to the <see cref="XElement"/> and it's decendant <see cref="XElement"/>s
        /// </summary>
        /// <param name="element">The <see cref="XElement"/></param>
        /// <param name="languageSelector">Select the prioper language for an element</param>
        /// <returns>The duration of the synthesized speech</returns>
        TimeSpan SynthesizeElement(XElement element, Func<XElement, CultureInfo> languageSelector);


        /// <summary>
        /// Gets metadata about the voices supported by the synthesizer
        /// </summary>
        /// <returns></returns>
        IEnumerable<VoiceMetaData> GetVoices();

        /// <summary>
        /// Determines if a given voice is supported by the synthesizer
        /// </summary>
        /// <param name="voice">A <see cref="VoiceMetaData"/> describing the voice</param>
        /// <returns>A <see cref="bool"/> indicating if the voice is supported</returns>
        bool IsVoiceSupported(VoiceMetaData voice);
    }
}
