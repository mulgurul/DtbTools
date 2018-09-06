using System;
using System.Xml.Linq;

namespace DtbSynthesizerLibrary.Xml
{
    public class SyncAnnotation
    {
        public XText Text { get; set; } = null;

        public XElement Element { get; set; }

        public string Src { get; set; } = "";

        public TimeSpan ClipBegin { get; set; } = TimeSpan.Zero;

        public TimeSpan ClipEnd { get; set; } = TimeSpan.Zero;
    }
}
