using System;
using System.Linq;
using System.Xml.Linq;

namespace DtbMerger2Library.Actions
{
    public class DeleteEntryAction : IAction
    {
        public XElement ElementToDelete { get; }

        private XElement parent = null;
        private int index = -1;


        public DeleteEntryAction(XElement elementToDelete)
        {
            ElementToDelete = elementToDelete;
        }

        public void Execute()
        {
            parent = ElementToDelete.Parent;
            index = parent?.Elements().ToList().IndexOf(ElementToDelete)??0;
            ElementToDelete.Remove();
        }

        public void UnExecute()
        {
            if (index == parent.Elements().Count())
            {
                parent.Add(ElementToDelete);
            }
            else
            {
                parent.Elements().ToList()[index].AddBeforeSelf(ElementToDelete);
            }
        }

        public Boolean CanExecute => ElementToDelete?.Parent != null;

        public Boolean CanUnExecute => parent != null && 0 <= index && index <= parent.Elements().Count();

        public String Description => "Delete entry";
    }
}