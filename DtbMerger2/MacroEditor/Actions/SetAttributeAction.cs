using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace MacroEditor.Actions
{
    public class SetAttributeAction : IAction
    {
        public XElement ElementToSetAttributeOn { get; }

        public XName AttributeName { get; }

        public string NewValue { get; }

        public string OldValue { get; }

        public SetAttributeAction(XElement elementToSetAttributeOn, XName attributeName, string newValue)
        {
            ElementToSetAttributeOn = elementToSetAttributeOn ?? throw new ArgumentNullException(nameof(elementToSetAttributeOn));
            AttributeName = attributeName ?? throw new ArgumentNullException(nameof(attributeName));
            NewValue = newValue;
            OldValue = ElementToSetAttributeOn.Attribute(attributeName)?.Value;
        }

        public void Execute()
        {
            ElementToSetAttributeOn.SetAttributeValue(AttributeName, NewValue);
        }

        public void UnExecute()
        {
            ElementToSetAttributeOn.SetAttributeValue(AttributeName, OldValue);
        }

        public bool CanExecute => true;
        public bool CanUnExecute => true;
        public string Description => "Set attribute";
    }
}
