using System;
using System.Xml.Linq;

namespace DtbMerger2Library.Actions
{
    public class RenameElementAction : IAction
    {
        public XName NewName { get; }

        public XName OldName { get; }

        public XElement ElementToRename { get; }

        public RenameElementAction(XElement elementToRename, XName newName)
        {
            ElementToRename = elementToRename ?? throw new ArgumentNullException(nameof(elementToRename));
            NewName = newName ?? throw new ArgumentNullException(nameof(newName));
            OldName = elementToRename.Name;
        }

        public void Execute()
        {
            ElementToRename.Name = NewName;
        }

        public void UnExecute()
        {
            ElementToRename.Name = OldName;
        }

        public bool CanExecute => true;
        public bool CanUnExecute => true;
        public string Description => "Rename entry";
    }
}
