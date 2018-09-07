using System;
using System.Xml.Linq;

namespace DtbMerger2Library.Actions
{
    /// <summary>
    /// Sets an attribute on an <see cref="XElement"/>
    /// </summary>
    public class SetAttributeAction : IAction
    {
        /// <summary>
        /// The <see cref="XElement"/> to set an attribute on
        /// </summary>
        public XElement ElementToSetAttributeOn { get; }

        /// <summary>
        /// The <see cref="XName"/> of the atribute to set
        /// </summary>
        public XName AttributeName { get; }

        /// <summary>
        /// The new value of the attribute
        /// </summary>
        public string NewValue { get; }

        /// <summary>
        /// The old value of the attribute before setting
        /// </summary>
        public string OldValue { get; }

        /// <summary>
        /// Constructor setting the <see cref="ElementToSetAttributeOn"/>, the <see cref="AttributeName"/> and the <see cref="NewValue"/>
        /// </summary>
        /// <param name="elementToSetAttributeOn">The <see cref="ElementToSetAttributeOn"/></param>
        /// <param name="attributeName">The <see cref="AttributeName"/></param>
        /// <param name="newValue">The <see cref="NewValue"/></param>
        public SetAttributeAction(XElement elementToSetAttributeOn, XName attributeName, string newValue)
        {
            ElementToSetAttributeOn = elementToSetAttributeOn ?? throw new ArgumentNullException(nameof(elementToSetAttributeOn));
            AttributeName = attributeName ?? throw new ArgumentNullException(nameof(attributeName));
            NewValue = newValue;
            OldValue = ElementToSetAttributeOn.Attribute(attributeName)?.Value;
        }

        /// <inheritdoc />
        public void Execute()
        {
            ElementToSetAttributeOn.SetAttributeValue(AttributeName, NewValue);
        }

        /// <inheritdoc />
        public void UnExecute()
        {
            ElementToSetAttributeOn.SetAttributeValue(AttributeName, OldValue);
        }

        /// <inheritdoc />
        public bool CanExecute => true;

        /// <inheritdoc />
        public bool CanUnExecute => true;

        /// <inheritdoc />
        public string Description => "Set attribute";
    }
}
