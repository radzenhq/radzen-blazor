using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Components.Forms;

namespace Radzen.Blazor.Rendering
{
    public class ClassList
    {
        private readonly StringBuilder builder = new StringBuilder();

        private ClassList(string className = null)
        {
            Add(className);
        }

        public static ClassList Create(string className = null) => new ClassList(className);

        public ClassList Add(string className, bool condition = true)
        {
            if (condition && !string.IsNullOrWhiteSpace(className))
            {
                if (builder.Length > 0)
                {
                    builder.Append(" ");
                }

                builder.Append(className);
            }

            return this;
        }

        public ClassList Add(IDictionary<string, object> attributes)
        {
            if (attributes != null && attributes.TryGetValue("class", out var className) && className != null)
            {
                return Add(className.ToString());
            }

            return this;
        }

        public ClassList Add(FieldIdentifier field, EditContext context)
        {
            if (field.FieldName != null && context != null)
            {
                return Add(context.FieldCssClass(field));
            }

            return this;
        }

        public ClassList AddDisabled(bool condition = true)
        {
            return Add("rz-state-disabled", condition);
        }

        public override string ToString()
        {
            return builder.ToString();
        }
    }
}