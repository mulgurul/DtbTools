using System;
using System.Linq;
using System.Xml.Linq;

namespace DtbMerger2Library.Actions
{
    /// <summary>
    /// Action that deletes an <see cref="XElement"/> from a <see cref="XDocument"/>
    /// </summary>
    public class DeleteElementsAction : IAction
    {
        /// <summary>
        /// The <see cref="XElement"/> to delete
        /// </summary>
        public XElement ElementToDelete { get; }

        private XElement parent = null;
        private int index = -1;

        /// <summary>
        /// Constructor setting the <see cref="XElement"/> to delete
        /// </summary>
        /// <param name="elementToDelete"></param>
        public DeleteElementsAction(XElement elementToDelete)
        {
            ElementToDelete = elementToDelete;
        }

        /// <inheritdoc />
        public void Execute()
        {
            parent = ElementToDelete.Parent;
            index = parent?.Elements().ToList().IndexOf(ElementToDelete)??0;
            ElementToDelete.Remove();
        }

        /// <inheritdoc />
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

        /// <inheritdoc />
        public Boolean CanExecute => ElementToDelete?.Parent != null;

        /// <inheritdoc />
        public Boolean CanUnExecute => parent != null && 0 <= index && index <= parent.Elements().Count();

        /// <inheritdoc />
        public String Description => "Delete entry";
    }
}