using System;
using System.Collections.Generic;
using System.Globalization;

namespace DtbSynthesizerLibrary
{
    /// <summary>
    /// Metadata about a voice
    /// </summary>
    public class VoiceMetaData : IEquatable<VoiceMetaData>
    {
        public VoiceMetaData(string type, string id)
        {
            Type = type ?? throw new ArgumentNullException(nameof(type));
            Id = id ?? throw new ArgumentNullException(nameof(id));
        }

        /// <summary>
        /// The name of the voice
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The unique identifier for the voice
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// The type of the voice. The following values are supported:
        /// <list type="bullet">
        ///     <item><term>Microsoft.Speech</term> <description>For voices based on <see cref="Microsoft.Speech.Synthesis.SpeechSynthesizer"/></description></item>
        ///     <item><term>System.Speech</term>    <description>For voices based on <see cref="System.Speech.Synthesis.SpeechSynthesizer"/></description></item>
        ///     <item><term>Google.Cloud</term>     <description>For voices based on <see cref="Google.Cloud.TextToSpeech.V1.TextToSpeechClient"/></description></item>
        ///     <item><term>Amazon.Polly</term>     <description>For voices based on <see cref="Amazon.Polly.AmazonPollyClient"/></description></item>
        /// </list>
        /// </summary>
        public string Type { get; }

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

        /// <summary>
        /// Gets a human readable description of the voice
        /// </summary>
        public string Description => $"{Name} ({Type} {Culture.DisplayName} voice with id {Id})";

        public bool Equals(VoiceMetaData other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(Id, other.Id, StringComparison.InvariantCultureIgnoreCase) && string.Equals(Type, other.Type, StringComparison.InvariantCultureIgnoreCase);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((VoiceMetaData) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Id != null ? StringComparer.InvariantCultureIgnoreCase.GetHashCode(Id) : 0) * 397) ^ (Type != null ? StringComparer.InvariantCultureIgnoreCase.GetHashCode(Type) : 0);
            }
        }
    }
}
