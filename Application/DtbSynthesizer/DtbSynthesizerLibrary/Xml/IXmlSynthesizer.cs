using System;
using System.Xml.Linq;
using NAudio.Wave;

namespace DtbSynthesizerLibrary.Xml
{
    /// <summary>
    /// Interface for a speech synthesizer, that can generate synthetic speech for <see cref="XElement"/>s
    /// </summary>
    public interface IXmlSynthesizer
    {
        /// <summary>
        /// Generates synthetic speech for an <see cref="XElement"/> and all it's decendants, 
        /// adding <see cref="SyncAnnotation"/> to the <see cref="XElement"/> and it's decendant <see cref="XElement"/>s
        /// </summary>
        /// <param name="element">The <see cref="XElement"/></param>
        /// <param name="writer">The <see cref="WaveFileWriter"/> to write the synthetic speech to</param>
        /// <param name="src">The value for the added <see cref="SyncAnnotation.Src"/>s</param>
        /// <returns>The duration of the synthesized speech</returns>
        TimeSpan SynthesizeElement(XElement element, WaveFileWriter writer, string src = "");

        /// <summary>
        /// Information about the voice used by the <see cref="IXmlSynthesizer"/>
        /// </summary>
        VoiceMetaData VoiceInfo { get; }

        /// <summary>
        /// The prefered (PCM) sample rate of the <see cref="IXmlSynthesizer"/> (or rather the voice used)
        /// </summary>
        int PreferedSampleRate { get; }
    }
}
