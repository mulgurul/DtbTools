using System;
using System.Linq;
using System.Xml.Linq;

namespace DtbMerger2Library.Actions
{

    /// <summary>
    /// Action that moves an <see cref="XElement"/> out,
    /// that is adds is as as a sibling imeadeatley following it's parent
    /// </summary>
    public class MoveElementOutAction : IAction
    {
        /// <summary>
        /// The <see cref="XElement"/> to move out
        /// </summary>
        public XElement ElementToMove { get; }

        /// <summary>
        /// The parent element of the <see cref="ElementToMove"/> (before the move)
        /// </summary>
        public XElement ParentElementBeforeMove { get; }

        /// <summary>
        /// The index of the <see cref="ElementToMove"/> in the list of children of the parent
        /// </summary>
        public int IndexBeforeMove { get; private set; }

        /// <summary>
        /// Constructor setting the <see cref="ElementToMove"/>
        /// </summary>
        /// <param name="elementToMove">The <see cref="ElementToMove"/></param>
        public MoveElementOutAction(XElement elementToMove)
        {
            ElementToMove = elementToMove;
            ParentElementBeforeMove = ElementToMove.Parent;
            IndexBeforeMove = ParentElementBeforeMove?.Elements().ToList().IndexOf(ElementToMove) ?? 0;
        }

        /// <inheritdoc />
        public void Execute()
        {
            ElementToMove.Remove();
            ParentElementBeforeMove.AddAfterSelf(ElementToMove);
        }

        /// <inheritdoc />
        public void UnExecute()
        {
            ElementToMove.Remove();
            if (IndexBeforeMove == ParentElementBeforeMove.Elements().Count())
            {
                ParentElementBeforeMove.Add(ElementToMove);
            }
            else
            {
                ParentElementBeforeMove.Elements().ToList()[IndexBeforeMove].AddBeforeSelf(ElementToMove);
            }
        }

        /// <inheritdoc />
        public Boolean CanExecute => ParentElementBeforeMove?.Parent != null;

        /// <inheritdoc />
        public Boolean CanUnExecute => true;

        /// <inheritdoc />
        public String Description => "Move element out";
    }
}
