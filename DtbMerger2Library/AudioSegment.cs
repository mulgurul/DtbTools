using System;
using System.Collections.Generic;
using System.Text;

namespace DtbMerger2Library
{
    public class AudioSegment
    {
        public Uri AudioFile { get; set; }

        public TimeSpan ClipBegin { get; set; }

        public TimeSpan ClipEnd { get; set; }

        public TimeSpan Duration => ClipEnd.Subtract(ClipBegin);
    }
}
