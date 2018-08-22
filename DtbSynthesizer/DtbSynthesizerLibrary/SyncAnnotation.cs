using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace DtbSynthesizerLibrary
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
