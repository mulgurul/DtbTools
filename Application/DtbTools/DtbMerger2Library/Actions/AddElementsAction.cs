using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace DtbMerger2Library.Actions
{
    /// <summary>
    /// Action, that adds one or more <see cref="XElement"/>s to a <see cref="XDocument"/>
    /// </summary>
    public class AddElementsAction : IAction
    {
        /// <summary>
        /// Gets the mode defining where to add the new element, relative to the <see cref="ContextElement"/>
        /// </summary>
        public AddModes AddMode { get; }

        private readonly List<XElement> elementsToAdd;

        /// <summary>
        /// The context <see cref="XElement"/> is relation to which the new element is added
        /// </summary>
        public XElement ContextElement { get; }

        /// <summary>
        /// The <see cref="XElement"/>s to add
        /// </summary>
        public IEnumerable<XElement> ElementsToAdd => elementsToAdd?.AsReadOnly();

        /// <summary>
        /// Constructor setting the <see cref="ContextElement"/>, <see cref="ElementsToAdd"/> 
        /// and <see cref="AddMode"/> of the action. 
        /// Optionally also sets the <see cref="Description"/> of the action
        /// </summary>
        /// <param name="contextElement">The context <see cref="XElement"/></param>
        /// <param name="elementsToAdd">The <see cref="XElement"/>s to add</param>
        /// <param name="addMode">The <see cref="AddMode"/></param>
        /// <param name="description">The optional description</param>
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

        /// <inheritdoc />
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

        /// <inheritdoc />
        public void UnExecute()
        {
            foreach (var elem in elementsToAdd)
            {
                elem.Remove();
            }
        }

        /// <inheritdoc />
        public bool CanExecute { get; }
        /// <inheritdoc />
        public bool CanUnExecute => true;
        /// <inheritdoc />
        public string Description { get; }
    }
}
