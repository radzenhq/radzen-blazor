using System;

namespace Radzen.Blazor
{
    [AttributeUsage(AttributeTargets.Property)]
    internal class CustomJsOptionAttribute : Attribute
    {
        public string PropertyName { get; private set; }
        public CustomJsOptionAttribute(string propertyName)
            : base()
        {
            PropertyName = propertyName;
        }
    }
}
