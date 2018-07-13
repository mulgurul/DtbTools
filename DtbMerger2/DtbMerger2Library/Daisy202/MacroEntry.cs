using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using DtbMerger2Library.Tree;

namespace DtbMerger2Library.Daisy202
{
    public class MacroEntry
    {
        public static IEnumerable<XElement> GetMacroElementsFromNcc(Uri nccUri)
        {
            return GetMacroElementsFromNcc(XDocument.Load(
                Uri.UnescapeDataString(nccUri.AbsolutePath),
                LoadOptions.SetBaseUri | LoadOptions.SetLineInfo));
        }
        public static IEnumerable<XElement> GetMacroElementsFromNcc(XDocument ncc)
        {
            return MergeEntry.LoadMergeEntriesFromNcc(ncc).Select(me => me.MacroElement);
        }

        public static IEnumerable<MacroEntry> GetMacroEntriesFromNcc(Uri nccUri)
        {
            return GetMacroElementsFromNcc(nccUri).Select(elem => new MacroEntry(elem));
        }

        public static IEnumerable<MacroEntry> GetMacroEntriesFromNcc(XDocument ncc)
        {
            return GetMacroElementsFromNcc(ncc).Select(elem => new MacroEntry(elem));
        }

        public XElement SourceElement { get; private set; }

        public string File => SourceElement.Attribute("file")?.Value;

        public string ItemId => SourceElement.Attribute("ItemID")?.Value;

        private string heading = null;

        public string Heading
        {
            get
            {
                if (heading == null)
                {
                    var mergeEntry = MergeEntry.LoadMergeEntriesFromMacroElement(SourceElement, false).FirstOrDefault();
                    heading = mergeEntry?.GetNccElements().FirstOrDefault()?.Value ?? SourceElement.ToString();
                }

                return heading;
            }
        }

        public void Refresh()
        {
            heading = null;
        }

        public MacroEntry(XElement source)
        {
            SourceElement = source;
        }

        public override String ToString()
        {
            return Heading;
        }
    }
}
