using System;
using System.Linq;
using System.Xml.Linq;

namespace DtbMerger2Library.Actions
{
    /// <summary>
    /// Action that moves an <see cref="XElement"/> in an <see cref="XDocument"/> down,
    /// that is moves the element one down the list of it's siblings
    /// </summary>
    public class MoveElementDownAction : IAction
    {
        /// <summary>
        /// The <see cref="XElement"/> to move down
        /// </summary>
        public XElement ElementToMove { get; }

        /// <summary>
        /// Constructor setting the <see cref="XElement"/> to move down
        /// </summary>
        /// <param name="elementToMove">The <see cref="XElement"/> to move down</param>
        public MoveElementDownAction(XElement elementToMove)
        {
            ElementToMove = elementToMove;
        }

        /// <inheritdoc />
        public void Execute()
        {
            var followingElement = ElementToMove.ElementsAfterSelf().First();
            ElementToMove.Remove();
            followingElement.AddAfterSelf(ElementToMove);
        }

        /// <inheritdoc />
        public void UnExecute()
        {
            var prevElement = ElementToMove.ElementsBeforeSelf().Last();
            ElementToMove.Remove();
            prevElement.AddBeforeSelf(ElementToMove);
        }

        /// <inheritdoc />
        public bool CanExecute => ElementToMove.ElementsAfterSelf().Any();

        /// <inheritdoc />
        public bool CanUnExecute => ElementToMove.ElementsBeforeSelf().Any();

        /// <inheritdoc />
        public String Description => "Move element down";
    }
}