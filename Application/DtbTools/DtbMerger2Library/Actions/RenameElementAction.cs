using System;
using System.Xml.Linq;

namespace DtbMerger2Library.Actions
{
    /// <summary>
    /// Action that renames an <see cref="XElement"/>
    /// </summary>
    public class RenameElementAction : IAction
    {
        /// <summary>
        /// The new <see cref="XName"/>
        /// </summary>
        public XName NewName { get; }

        /// <summary>
        /// The old <see cref="XName"/>, the <see cref="ElementToRename"/> had before
        /// </summary>
        public XName OldName { get; }

        /// <summary>
        /// The <see cref="XElement"/> to rename
        /// </summary>
        public XElement ElementToRename { get; }

        /// <summary>
        /// Constructor setting the <see cref="ElementToRename"/> and the <see cref="NewName"/>
        /// </summary>
        /// <param name="elementToRename">The <see cref="ElementToRename"/></param>
        /// <param name="newName">The <see cref="NewName"/></param>
        public RenameElementAction(XElement elementToRename, XName newName)
        {
            ElementToRename = elementToRename ?? throw new ArgumentNullException(nameof(elementToRename));
            NewName = newName ?? throw new ArgumentNullException(nameof(newName));
            OldName = elementToRename.Name;
        }

        /// <inheritdoc />
        public void Execute()
        {
            ElementToRename.Name = NewName;
        }

        /// <inheritdoc />
        public void UnExecute()
        {
            ElementToRename.Name = OldName;
        }

        /// <inheritdoc />
        public bool CanExecute => true;

        /// <inheritdoc />
        public bool CanUnExecute => true;

        /// <inheritdoc />
        public string Description => "Rename entry";
    }
}
