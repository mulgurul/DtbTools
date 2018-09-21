using System;
using System.Linq;
using System.Xml.Linq;

namespace DtbMerger2Library.Actions
{
    /// <summary>
    /// Action that moves an <see cref="XElement"/> in an <see cref="XDocument"/> up,
    /// that is moves the element one up the list of it's siblings
    /// </summary>
    public class MoveElementUpAction : IAction
    {
        /// <summary>
        /// The <see cref="XElement"/> to move up
        /// </summary>
        public XElement ElementToMove { get; }

        /// <summary>
        /// Constructor setting the <see cref="XElement"/> to move up
        /// </summary>
        /// <param name="elementToMove">The <see cref="XElement"/> to move up</param>
        public MoveElementUpAction(XElement elementToMove)
        {
            ElementToMove = elementToMove;
        }

        /// <inheritdoc />
        public void Execute()
        {
            var prevElement = ElementToMove.ElementsBeforeSelf().Last();
            ElementToMove.Remove();
            prevElement.AddBeforeSelf(ElementToMove);
        }

        /// <inheritdoc />
        public void UnExecute()
        {
            var followingElement = ElementToMove.ElementsAfterSelf().First();
            ElementToMove.Remove();
            followingElement.AddAfterSelf(ElementToMove);
        }

        /// <inheritdoc />
        public bool CanExecute => ElementToMove.ElementsBeforeSelf().Any();

        /// <inheritdoc />
        public bool CanUnExecute => ElementToMove.ElementsAfterSelf().Any();

        /// <inheritdoc />
        public String Description => "Move entry up";
    }
}