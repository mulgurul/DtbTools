using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace DtbMerger2Library.Actions
{
    public class AddElementsAction : IAction
    {
        public AddModes AddMode { get; }

        private readonly List<XElement> elementsToAdd;

        public XElement ContextElement { get; }

        public IEnumerable<XElement> ElementsToAdd => elementsToAdd?.AsReadOnly();

        public AddElementsAction(XElement contextElement, IEnumerable<XElement> elementsToAdd, AddModes addMode, string description = "Add entries")
        {
            ContextElement = contextElement;
            this.elementsToAdd = new List<XElement>(elementsToAdd);
            Description = description;
            AddMode = addMode;
            switch (addMode)
            {
                case AddModes.AddAsChildren:
                    CanExecute = contextElement != null;
                    break;
                case AddModes.InsertBefore:
                    CanExecute = contextElement?.Parent != null;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(addMode), addMode, null);
            }
        }
        public void Execute()
        {
            switch (AddMode)
            {
                case AddModes.AddAsChildren:
                    ContextElement.Add(elementsToAdd);
                    break;
                case AddModes.InsertBefore:
                    ContextElement.AddBeforeSelf(elementsToAdd);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void UnExecute()
        {
            foreach (var elem in elementsToAdd)
            {
                elem.Remove();
            }
        }

        public bool CanExecute { get; }
        public bool CanUnExecute => true;
        public string Description { get; }
    }
}
