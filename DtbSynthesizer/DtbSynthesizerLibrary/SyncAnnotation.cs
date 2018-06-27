using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DtbSynthesizerLibrary
{
    public class SyncAnnotation
    {
        public TimeSpan ClipBegin { get; set; } = TimeSpan.MinValue;

        public TimeSpan ClipEnd { get; set; } = TimeSpan.MinValue;
    }
}
