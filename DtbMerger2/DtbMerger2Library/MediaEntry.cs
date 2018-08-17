using System;
using System.Collections.Generic;
using System.Text;

namespace DtbMerger2Library
{
    /// <summary>
    /// Represents a media entry (typically an image - not audio files referenced by smil files)
    /// </summary>
    public class MediaEntry : IEquatable<MediaEntry>
    {
        /// <summary>
        /// The <see cref="Uri"/> of the media file
        /// </summary>
        public Uri Source { get; set; }

        /// <summary>
        /// The <see cref="Uri"/> of the media file, relative to the DTB to which it belongs
        /// </summary>
        public Uri RelativeUri { get; set; }

        /// <inheritdoc />
        public bool Equals(MediaEntry other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(Source, other.Source) && Equals(RelativeUri, other.RelativeUri);
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((MediaEntry) obj);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                return ((Source != null ? Source.GetHashCode() : 0) * 397) ^ (RelativeUri != null ? RelativeUri.GetHashCode() : 0);
            }
        }
    }
}
