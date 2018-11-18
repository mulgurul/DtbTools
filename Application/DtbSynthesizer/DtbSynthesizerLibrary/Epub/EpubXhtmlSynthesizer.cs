using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using DtbSynthesizerLibrary.Xhtml;
using DtbSynthesizerLibrary.Xml;
using NAudio.Wave;

namespace DtbSynthesizerLibrary.Epub
{
    public class EpubXhtmlSynthesizer : AbstractXhtmlSynthesizer
    {
        public event EventHandler<XElementReachedEventArgs> ElementReached;

        public static XNamespace Smil30Ns = "http://www.w3.org/ns/SMIL";
        public static XNamespace EpubOpsNs = "http://www.idpf.org/2007/ops";

        public static IEnumerable<XElement> GetSmil30ElementFromXhtmlElement(XElement element)
        {
            if (String.IsNullOrEmpty(element.Attribute("id")?.Value))
            {
                return element.Elements().SelectMany(GetSmil30ElementFromXhtmlElement);
            }
            var sync = element.Annotation<SyncAnnotation>();
            var textref = $"{Utils.GetFileName(element)}#{element.Attribute("id")?.Value}";
            var smilElem =
                sync != null
                    ? new XElement(
                        Smil30Ns + "par",
                        new XElement(Smil30Ns + "text", new XAttribute("src", textref)),
                        new XElement(
                            Smil30Ns + "audio", 
                            new XAttribute("src", sync.Src),
                            new XAttribute("clipBegin", $"{sync.ClipBegin.TotalSeconds.ToString("F3", CultureInfo.InvariantCulture)}s"),
                            new XAttribute("clipEnd", $"{sync.ClipEnd.TotalSeconds.ToString("F3", CultureInfo.InvariantCulture)}s")))
                    : new XElement(
                        Smil30Ns + "seq", 
                        new XAttribute(EpubOpsNs + "textref", textref),
                        element.Elements().SelectMany(GetSmil30ElementFromXhtmlElement));
            if (!String.IsNullOrEmpty(element.Attribute(EpubOpsNs + "type")?.Value))
            {
                smilElem.SetAttributeValue(EpubOpsNs+"type", element.Attribute(EpubOpsNs + "type")?.Value);
            }
            return new[] {smilElem};
        }

        public XDocument MediaOverlayDocument => new XDocument(
            new XDeclaration("1.0", "utf-8", "1"),
            new XElement(
                Smil30Ns + "smil",
                new XAttribute("version", "3.0"),
                new XElement(
                    Smil30Ns+"body",
                    new XAttribute(EpubOpsNs + "type", Body.Attribute(EpubOpsNs+"type")?.Value??""),
                    Body.Elements().SelectMany(GetSmil30ElementFromXhtmlElement))
            ));

        public WaveFileWriter AudioWriter { get; set; }

        public string AudioFileSrc { get; set; }


        public override bool Synthesize()
        {
            var elements = ElementsToSynthesize;
            for (int i = 0; i < elements.Count; i++)
            {
                var elem = elements[i];
                if (FireProgress(
                    100 * i / elements.Count,
                    $"Synthesizing element {i + 1} of {elements.Count} to {AudioFileSrc}"))
                {
                    return false;
                }
                ElementReached?.Invoke(this, new XElementReachedEventArgs() { Element = elem });
                var ci = Utils.SelectCulture(elem);
                var synth = CultureInfo.InvariantCulture.Equals(ci)
                    ? DefaultSynthesizer
                    : SynthesizerSelector(ci);
                synth.TextToSynthesizeDelegate = e => e.Value;
                synth.SynthesizeElement(elem, AudioWriter, AudioFileSrc);
            }
            return true;
        }

    }
}

namespace DtbSynthesizerLibrary.Xhtml
{
    public class XElementReachedEventArgs : EventArgs
    {
        public XElement Element { get; set; }
    }
}
