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

        public MoveEntryInAction(XElement elementToMove)
        {
            ElementToMove = elementToMove;
        }

        public void Execute()
        {
            throw new NotImplementedException();
        }

        public void UnExecute()
        {
            throw new NotImplementedException();
        }

        public Boolean CanExecute => false;

        public Boolean CanUnExecute => false;

        public String Description => "Move entry in";
    }
}
