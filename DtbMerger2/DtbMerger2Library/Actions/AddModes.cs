using System.Xml.Linq;

namespace DtbMerger2Library.Actions
{
    /// <summary>
    /// The possible modes for adding <see cref="XElement"/>s to a <see cref="XDocument"/> in relation to a context <see cref="XElement"/>
    /// </summary>
    public enum AddModes
    {
        /// <summary>
        /// Adds the <see cref="XElement"/>s as children of the context <see cref="XElement"/>
        /// </summary>
        AddAsChildren,
        /// <summary>
        /// Inserts the <see cref="XElement"/>s as preceding siblings of the context <see cref="XElement"/> (that is before)
        /// </summary>
        InsertBefore
    }
}