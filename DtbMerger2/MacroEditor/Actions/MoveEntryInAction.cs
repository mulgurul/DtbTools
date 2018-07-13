using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace MacroEditor.Actions
{
    public class MoveEntryInAction : IAction
    {
        public XElement ElementToMove { get; }

        public XElement PreviousElement { get; }

        public MoveEntryInAction(XElement elementToMove)
        {
            ElementToMove = elementToMove;
            PreviousElement = ElementToMove.ElementsBeforeSelf().LastOrDefault();
        }

        public void Execute()
        {
            ElementToMove.Remove();
            PreviousElement.Add(ElementToMove);
        }

        public void UnExecute()
        {
            ElementToMove.Remove();
            PreviousElement.AddAfterSelf(ElementToMove);
        }

        public Boolean CanExecute => PreviousElement != null;

        public Boolean CanUnExecute => PreviousElement != null;

        public String Description => "Move entry in";
    }
}
