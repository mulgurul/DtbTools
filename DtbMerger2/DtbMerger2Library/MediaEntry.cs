using System;
using System.Collections.Generic;
using System.Text;

namespace DtbMerger2Library
{
    public class MediaEntry : IEquatable<MediaEntry>
    {
        public Uri Source { get; set; }

        public Uri RelativeUri { get; set; }

        public bool Equals(MediaEntry other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(Source, other.Source) && Equals(RelativeUri, other.RelativeUri);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((MediaEntry) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Source != null ? Source.GetHashCode() : 0) * 397) ^ (RelativeUri != null ? RelativeUri.GetHashCode() : 0);
            }
        }
    }
}
