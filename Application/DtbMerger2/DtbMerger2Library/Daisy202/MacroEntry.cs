using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using DtbMerger2Library.Tree;

namespace DtbMerger2Library.Daisy202
{
    /// <summary>
    /// Represents an entry in a macro file
    /// </summary>
    public class MacroEntry
    {
        /// <summary>
        /// Gets macro <see cref="XElement"/>s representing an entire DTB
        /// </summary>
        /// <param name="nccUri">The <see cref="Uri"/> of the ncc of the DTB</param>
        /// <returns>The macro <see cref="XElement"/>s representing the DTB</returns>
        public static IEnumerable<XElement> GetMacroElementsFromNcc(Uri nccUri)
        {
            return GetMacroElementsFromNcc(Utils.LoadXDocument(nccUri));
        }

        /// <summary>
        /// Gets macro <see cref="XElement"/>s representing an entire DTB
        /// </summary>
        /// <param name="ncc">The ncc <see cref="XDocument"/> of the DTB - must have <see cref="XObject.BaseUri"/> set</param>
        /// <returns>The macro <see cref="XElement"/>s representing the DTB</returns>
        public static IEnumerable<XElement> GetMacroElementsFromNcc(XDocument ncc)
        {
            return MergeEntry.LoadMergeEntriesFromNcc(ncc).Select(me => me.MacroElement);
        }

        /// <summary>
        /// Gets the <see cref="MacroEntry"/>s representing an entire DTB
        /// </summary>
        /// <param name="nccUri">The <see cref="Uri"/> of the ncc of the DTB</param>
        /// <returns>The <see cref="MacroEntry"/>s representing the DTB</returns>
        public static IEnumerable<MacroEntry> GetMacroEntriesFromNcc(Uri nccUri)
        {
            return GetMacroElementsFromNcc(nccUri).Select(elem => new MacroEntry(elem));
        }

        /// <summary>
        /// Gets the <see cref="MacroEntry"/>s representing an entire DTB
        /// </summary>
        /// <param name="ncc">The ncc <see cref="XDocument"/> of the DTB - must have <see cref="XObject.BaseUri"/> set</param>
        /// <returns>The <see cref="MacroEntry"/>s representing the DTB</returns>
        public static IEnumerable<MacroEntry> GetMacroEntriesFromNcc(XDocument ncc)
        {
            return GetMacroElementsFromNcc(ncc).Select(elem => new MacroEntry(elem));
        }

        /// <summary>
        /// The source <see cref="XElement"/> of the <see cref="MacroEntry"/>
        /// </summary>
        public XElement SourceElement { get; }

        private string heading = null;

        /// <summary>
        /// The heading of the <see cref="MacroEntry"/> - the value of the (first) ncc heading element pointed to by the macro entry
        /// </summary>
        public string Heading
        {
            get
            {
                if (heading == null)
                {
                    var mergeEntry = MergeEntry.LoadMergeEntriesFromMacroElement(SourceElement, false).FirstOrDefault();
                    heading = mergeEntry?.NccElements.FirstOrDefault()?.Value ?? SourceElement.ToString();
                }

                return heading;
            }
        }

        /// <summary>
        /// Refreshes the <see cref="MacroEntry"/> - mainly forces a new lazy loading of the <see cref="Heading"/>
        /// </summary>
        public void Refresh()
        {
            heading = null;
        }

        /// <summary>
        /// Constructor setting the <see cref="SourceElement"/>
        /// </summary>
        /// <param name="source">The <see cref="SourceElement"/></param>
        public MacroEntry(XElement source)
        {
            SourceElement = source;
        }

        /// <inheritdoc />
        public override String ToString()
        {
            return Heading;
        }
    }
}
