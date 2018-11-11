using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using DtbSynthesizerLibrary.Xml;

namespace DtbSynthesizerLibrary.Xhtml
{
    public class EpubXhtmlSynthesizer : XhtmlSynthesizer
    {

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

        public EpubXhtmlSynthesizer()
        {
            NewAudioFileAtHeading = false;
            TextToSynthesizeDelegate = e =>
                Utils.GetFirstNonEmpty(e.Value, e.Attribute("title")?.Value, e.Attribute("alt")?.Value);
        }

        public XDocument MediaOverlayDocument => new XDocument(
            new XElement(
                Smil30Ns + "smil",
                new XAttribute("version", "3.0"),
                new XElement(
                    Smil30Ns+"body",
                    new XAttribute(EpubOpsNs + "type", Body.Attribute(EpubOpsNs+"type")?.Value??""),
                    Body.Elements().SelectMany(GetSmil30ElementFromXhtmlElement))
            ));

    }
}
