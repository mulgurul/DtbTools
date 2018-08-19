using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using NAudio.Codecs;
using NAudio.Wave;

namespace DtbSynthesizerLibrary.Xhtml
{
    public class XhtmlSynthesizer
    {
        public static XNamespace XhtmlNs => "http://www.w3.org/1999/xhtml";

        protected bool FireProgress(int percentage, string message)
        {
            var d = Progress;
            if (d != null)
            {
                var a = new ProgressEventArgs(percentage, message);
                d(this, a);
                return a.Cancel;
            }
            return false;
        }

        public event EventHandler<ProgressEventArgs> Progress;

        public XDocument XhtmlDocument { get; set; }

        public XElement Body => XhtmlDocument?.Root?.Element(XhtmlNs + "body");

        public string XhtmlDirectory
        {
            get
            {
                var baseUri = XhtmlDocument?.BaseUri;
                if (baseUri == null)
                {
                    return null;
                }
                try
                {
                    return Path.GetDirectoryName(new Uri(baseUri).LocalPath);
                }
                catch (Exception e)
                {
                    throw new InvalidOperationException(
                        $"Could not get local directory from {nameof(XhtmlDocument)} BaseUri {baseUri}: {e.Message}");
                }
            }
        }

        public Func<CultureInfo, IXmlSynthesizer> SynthesizerSelector { get; set; } 
            = Utils.GetPrefferedXmlSynthesizerForCulture;

        public IXmlSynthesizer DefaultSynthesizer { get; set; }

        public WaveFormat AudioWaveFormat { get; set; } = new WaveFormat(44100, 1);

        private void ValidateSynthesizer()
        {
            if (XhtmlDocument == null)
            {
                throw new InvalidOperationException($"{nameof(XhtmlDocument)} property is null");
            }
            if (XhtmlDocument.BaseUri == null)
            {
                throw new InvalidOperationException($"{nameof(XhtmlDocument)} has no BaseUri");
            }
            if (Body == null)
            {
                throw new InvalidOperationException($"{nameof(XhtmlDocument)} has no body element");
            }
            if (Body.Descendants().Any(elem => Utils.SelectCulture(elem) == null) && DefaultSynthesizer == null)
            {
                throw new InvalidOperationException($"nameof{DefaultSynthesizer} is null");
            }
            var languages = Body.Descendants().Select(Utils.GetLanguage).Distinct().Where(lang => lang != null).ToList();
            if (languages.Any())
            {
                if (SynthesizerSelector == null)
                {
                    throw new InvalidOperationException($"{nameof(SynthesizerSelector)} is null");
                }
                var langsWithNoSynth = languages.Where(lang => SynthesizerSelector(new CultureInfo(lang)) == null)
                    .ToList();
                if (langsWithNoSynth.Any())
                {
                    var ls = langsWithNoSynth.Aggregate((s, v) => $"{s};{v}");
                    throw new InvalidOperationException($"No synthesizeres found for languages {ls}");
                }
            }
            if (
                Body.Descendants().Select(Utils.SelectCulture).Any(CultureInfo.InvariantCulture.Equals) 
                && DefaultSynthesizer == null)
            {
                throw new InvalidOperationException($"{nameof(DefaultSynthesizer)} is null");
            }
        }

        public IList<XName> BlockContainerNames { get; } = new List<XName>(new[]
        {
            "article",
            "aside",
            "div",
            "footer",
            "header",
            "main",
            "section"
        }.Select(n => XhtmlNs + n));

        public IReadOnlyList<XName> HeaderNames { get; }
            = new ReadOnlyCollection<XName>(Enumerable.Range(1, 6).Select(i => XhtmlNs + $"h{i}").ToList());

        private IEnumerable<XElement> ExpandBlockContainers(XElement elem)
        {
            if (elem == null) throw new ArgumentNullException(nameof(elem));
            if (BlockContainerNames?.Contains(elem.Name) ?? false)
            {
                if (elem.Nodes().OfType<XText>().Any(t => !String.IsNullOrWhiteSpace(t.Value)))
                {
                    var lineInfo = (IXmlLineInfo) elem;
                    throw new InvalidOperationException(
                        $"Div element with mixed or pure text content not supported ({lineInfo})");
                }
                return elem.Elements().SelectMany(ExpandBlockContainers);
            }
            return new[] {elem};
        }

        private int waveFileNumber;

        private string WaveFileName => $"AUD{waveFileNumber:D5}.wav";

        private string WaveFilePath => Path.Combine(
            XhtmlDirectory??Directory.GetCurrentDirectory(),
            WaveFileName);

        public bool Synthesize()
        {
            ValidateSynthesizer();
            waveFileNumber = -1;
            var dur = TimeSpan.Zero;
            WaveFileWriter writer = null;
            var elements = Body.Elements().SelectMany(ExpandBlockContainers).ToList();
            for (int i = 0; i < elements.Count; i++)
            {
                if (FireProgress(
                    100 * i / elements.Count,
                    $"Synthesizing element {i + 1} of {elements.Count} to {WaveFileName}"))
                {
                    return false;
                }
                var elem = elements[i];
                if (HeaderNames.Contains(elem.Name) || writer == null)
                {
                    writer?.Close();
                    waveFileNumber++;
                    writer = new WaveFileWriter(WaveFilePath, AudioWaveFormat);
                }
                var ci = Utils.SelectCulture(elem);
                var synth = CultureInfo.InvariantCulture.Equals(ci)
                    ? DefaultSynthesizer
                    : SynthesizerSelector(ci);
                dur += synth.SynthesizeElement(elem, writer, WaveFileName);
            }
            writer?.Close();
            return true;
        }
    }
}
