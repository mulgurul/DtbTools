using System;
using System.Collections.Generic;
using System.Text;
using DtbMerger2Library.Daisy202;

namespace DtbMerger2Library
{
    public class AudioSegment
    {
        private Uri audioFile;
        private TimeSpan? audioFileDuration;

        public Uri AudioFile
        {
            get => audioFile;
            set
            {
                audioFileDuration = null;
                audioFile = value;
            }
        }

        public TimeSpan ClipBegin { get; set; }

        public TimeSpan ClipEnd { get; set; }

        public TimeSpan Duration => ClipEnd.Subtract(ClipBegin);

        public TimeSpan AudioFileDuration
        {
            get
            {
                if (!audioFileDuration.HasValue)
                {
                    audioFileDuration = Utils.GetAudioFileDuration(AudioFile);
                }

                return audioFileDuration.Value;
            }
        }
    }
}
