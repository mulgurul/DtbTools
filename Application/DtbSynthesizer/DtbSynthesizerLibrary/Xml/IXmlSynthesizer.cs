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
        /// Generates synthetic speech for an <see cref="XElement"/> and all it's descendant, 
        /// adding <see cref="SyncAnnotation"/> to the <see cref="XElement"/> and it's descendant <see cref="XElement"/>s
        /// </summary>
        /// <param name="element">The <see cref="XElement"/></param>
        /// <param name="writer">The <see cref="WaveFileWriter"/> to write the synthetic speech to</param>
        /// <param name="src">The value for the added <see cref="SyncAnnotation.Src"/>s</param>
        /// <returns>The duration of the synthesized speech</returns>
        TimeSpan SynthesizeElement(XElement element, WaveFileWriter writer, string src = "");

        /// <summary>
        /// Generates synthetic speech for some text
        /// </summary>
        /// <param name="text">The text to synthesize</param>
        /// <param name="writer">The <see cref="WaveFileWriter"/> to write the synthetic speech to</param>
        /// <param name="src">The value for the added <see cref="SyncAnnotation.Src"/>s</param>
        /// <returns>A <see cref="SyncAnnotation"/> representing the synthesized text</returns>
        /// <remarks>
        /// The <see cref="SyncAnnotation.Element"/> on the returned <see cref="SyncAnnotation"/> is not set by this method
        /// </remarks>
        SyncAnnotation SynthesizeText(string text, WaveFileWriter writer, string src = "");

        /// <summary>
        /// A delegate to get the text to synthesize for a given element
        /// </summary>
        Func<XElement, string> TextToSynthesizeDelegate { get; set; }

        VoiceMetaData VoiceInfo { get; }
    }
}
