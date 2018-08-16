using System;
using System.Linq;
using System.Xml.Linq;

namespace DtbMerger2Library.Actions
{
    /// <summary>
    /// Action that moves an <see cref="XElement"/> in,
    /// that is adds is as the last child of it's preceding sibling <see cref="XElement"/>
    /// </summary>
    public class MoveElementInAction : IAction
    {
        /// <summary>
        /// The <see cref="XElement"/> to move in
        /// </summary>
        public XElement ElementToMove { get; }

        /// <summary>
        /// The previous <see cref="XElement"/>,
        /// that is the preceding sibling of <see cref="ElementToMove"/> before the move
        /// </summary>
        public XElement PreviousElement { get; }

        /// <summary>
        /// Constructor setting the <see cref="XElement"/> to move
        /// </summary>
        /// <param name="elementToMove">The <see cref="XElement"/> to move</param>
        public MoveElementInAction(XElement elementToMove)
        {
            ElementToMove = elementToMove;
            PreviousElement = ElementToMove.ElementsBeforeSelf().LastOrDefault();
        }

        /// <inheritdoc />
        public void Execute()
        {
            ElementToMove.Remove();
            PreviousElement.Add(ElementToMove);
        }

        /// <inheritdoc />
        public void UnExecute()
        {
            ElementToMove.Remove();
            PreviousElement.AddAfterSelf(ElementToMove);
        }

        /// <inheritdoc />
        public Boolean CanExecute => PreviousElement != null;

        /// <inheritdoc />
        public Boolean CanUnExecute => PreviousElement != null;

        /// <inheritdoc />
        public String Description => "Move element in";
    }
}
