using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Speech.Synthesis;

namespace DtbSynthesizerLibrary
{
    /// <summary>
    /// Metadata about a voice
    /// </summary>
    public class VoiceMetaData
    {
        /// <summary>
        /// The name of the voice
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The type of the voice.
        /// Values "Microsoft.Speech" and "System.Speech" indicates voices based 
        /// on <see cref="Microsoft.Speech.Synthesis.SpeechSynthesizer"/> 
        /// and <see cref="System.Speech.Synthesis.SpeechSynthesizer"/> respectively
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// The gender of the voice
        /// </summary>
        public string Gender { get; set; }

        /// <summary>
        /// The culture of the voice
        /// </summary>
        public CultureInfo Culture { get; set; }

        /// <summary>
        /// A <see cref="IDictionary{TKey,TValue}"/> containing additional information about the voice
        /// </summary>
        public IDictionary<string,string> AdditionalInfo { get; set; }

        public string Description => $"{Name} ({Culture}, {Type})";

        public override string ToString()
        {
            return Description;
        }
    }
}
