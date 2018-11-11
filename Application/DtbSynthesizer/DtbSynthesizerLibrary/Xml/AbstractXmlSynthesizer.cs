using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using NAudio.Wave;

namespace DtbSynthesizerLibrary.Xml
{
    public abstract class AbstractXmlSynthesizer : IXmlSynthesizer
    {
        public virtual TimeSpan SynthesizeElement(XElement element, WaveFileWriter writer, string src = "")
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            var syncAnno = SynthesizeText(element.Value, writer, src);
            syncAnno.Element = element;
            element.AddAnnotation(syncAnno);
            return syncAnno.ClipEnd.Subtract(syncAnno.ClipBegin);
        }
        public abstract SyncAnnotation SynthesizeText(string text, WaveFileWriter writer, string src = "");
        public Func<XElement, string> TextToSynthesizeDelegate { get; set; } = e => e.Value;
        public abstract VoiceMetaData VoiceInfo { get; }
    }
}
