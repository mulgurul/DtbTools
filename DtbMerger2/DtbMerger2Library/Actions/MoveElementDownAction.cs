using System;
using System.Linq;
using System.Xml.Linq;

namespace DtbMerger2Library.Actions
{
    public class MoveElementDownAction : IAction
    {
        public XElement ElementToMove { get; }

        public MoveElementDownAction(XElement elementToMove)
        {
            ElementToMove = elementToMove;
        }

        public void Execute()
        {
            var followingElement = ElementToMove.ElementsAfterSelf().First();
            ElementToMove.Remove();
            followingElement.AddAfterSelf(ElementToMove);
        }

        public void UnExecute()
        {
            var prevElement = ElementToMove.ElementsBeforeSelf().Last();
            ElementToMove.Remove();
            prevElement.AddBeforeSelf(ElementToMove);
        }

        public bool CanExecute => ElementToMove.ElementsAfterSelf().Any();

        public bool CanUnExecute => ElementToMove.ElementsBeforeSelf().Any();

        public String Description => "Move entry down";
    }
}