using System;
using System.Linq;
using System.Xml.Linq;

namespace DtbMerger2Library.Actions
{
    public class MoveEntryUpAction : IAction
    {
        public XElement ElementToMove { get; }

        public MoveEntryUpAction(XElement elementToMove)
        {
            ElementToMove = elementToMove;
        }

        public void Execute()
        {
            var prevElement = ElementToMove.ElementsBeforeSelf().Last();
            ElementToMove.Remove();
            prevElement.AddBeforeSelf(ElementToMove);
        }

        public void UnExecute()
        {
            var followingElement = ElementToMove.ElementsAfterSelf().First();
            ElementToMove.Remove();
            followingElement.AddAfterSelf(ElementToMove);
        }

        public bool CanExecute => ElementToMove.ElementsBeforeSelf().Any();

        public bool CanUnExecute => ElementToMove.ElementsAfterSelf().Any();

        public String Description => "Move entry up";
    }
}