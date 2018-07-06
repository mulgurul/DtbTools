using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace MacroEditor.Actions
{
    public class MoveEntryOutAction : IAction
    {
        public XElement ElementToMove { get; }

        public XElement ParentElementBeforeMove { get; private set; }

        public int IndexBeforeMove { get; private set; }

        public MoveEntryOutAction(XElement elementToMove)
        {
            ElementToMove = elementToMove;
            ParentElementBeforeMove = ElementToMove.Parent;
            IndexBeforeMove = ParentElementBeforeMove?.Elements().ToList().IndexOf(ElementToMove) ?? 0;
        }

        public void Execute()
        {
            ElementToMove.Remove();
            ParentElementBeforeMove.AddAfterSelf(ElementToMove);
        }

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

        public Boolean CanExecute => ParentElementBeforeMove?.Parent != null;

        public Boolean CanUnExecute => true;

        public String Description => "Move entry out";
    }
}
