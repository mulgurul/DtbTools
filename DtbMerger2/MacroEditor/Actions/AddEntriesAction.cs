using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace MacroEditor.Actions
{
    public class AddEntriesAction : IAction
    {
        public AddModes AddMode { get; }

        private readonly List<XElement> elementsToAdd;

        public XElement ContextElement { get; }

        public IEnumerable<XElement> ElementsToAdd => elementsToAdd?.AsReadOnly();

        public AddEntriesAction(XElement contextElement, IEnumerable<XElement> elementsToAdd, AddModes addMode, string description = "Add entries")
        {
            ContextElement = contextElement;
            this.elementsToAdd = new List<XElement>(elementsToAdd);
            Description = description;
            AddMode = addMode;
            switch (addMode)
            {
                case AddModes.AddAsChildren:
                    CanExecute = contextElement != null;
                    CanUnExecute = contextElement != null;
                    break;
                case AddModes.InsertBefore:
                    CanExecute = contextElement?.Parent != null;
                    CanUnExecute = contextElement?.Parent != null;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(addMode), addMode, null);
            }
        }
        public void Execute()
        {
            throw new NotImplementedException();
        }

        public void UnExecute()
        {
            throw new NotImplementedException();
        }

        public bool CanExecute { get; }
        public bool CanUnExecute { get; }
        public string Description { get; }
    }
}
