using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.Speech.AudioFormat;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using NAudio.Wave;

namespace DtbSynthesizerLibrary
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

        bool RecurseMixedContent { get; set; }

        VoiceMetaData VoiceInfo { get; }
    }
}
