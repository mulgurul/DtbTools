using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DtbSynthesizerLibrary
{
    public class SyncAnnotation
    {
        public string Src { get; set; } = "";

        public TimeSpan ClipBegin { get; set; } = TimeSpan.Zero;

        public TimeSpan ClipEnd { get; set; } = TimeSpan.Zero;
    }
}
