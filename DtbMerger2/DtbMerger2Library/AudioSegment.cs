using System;
using System.Collections.Generic;
using System.Text;
using DtbMerger2Library.Daisy202;

namespace DtbMerger2Library
{
    /// <summary>
    /// Represents an audio segment of a DTB, that is a clip of audio in an audio file defined by clip begin/end values
    /// </summary>
    public class AudioSegment
    {
        private Uri audioFile;
        private TimeSpan? audioFileDuration;

        /// <summary>
        /// Gets or sets the <see cref="Uri"/> of the audio file of the <see cref="AudioSegment"/>
        /// </summary>
        public Uri AudioFile
        {
            get => audioFile;
            set
            {
                audioFileDuration = null;
                audioFile = value;
            }
        }

        /// <summary>
        /// Gets or sets the clip begin value of the <see cref="AudioSegment"/>
        /// </summary>
        public TimeSpan ClipBegin { get; set; }

        /// <summary>
        /// Gets or sets the clip end value of the <see cref="AudioSegment"/>
        /// </summary>
        public TimeSpan ClipEnd { get; set; }

        /// <summary>
        /// Gets the duration of the <see cref="AudioSegment"/>
        /// </summary>
        public TimeSpan Duration => ClipEnd.Subtract(ClipBegin);

        /// <summary>
        /// Gets the duration of the audio file of the <see cref="AudioSegment"/>
        /// </summary>
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
