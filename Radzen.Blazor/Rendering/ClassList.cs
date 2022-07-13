using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Components.Forms;

namespace Radzen.Blazor.Rendering
{
    /// <summary>
    /// Class ClassList.
    /// </summary>
    public class ClassList
    {
        /// <summary>
        /// The builder
        /// </summary>
        private readonly StringBuilder builder = new StringBuilder();

        /// <summary>
        /// Initializes a new instance of the <see cref="ClassList"/> class.
        /// </summary>
        /// <param name="className">Name of the class.</param>
        private ClassList(string className = null)
        {
            Add(className);
        }

        /// <summary>
        /// Creates the specified class name.
        /// </summary>
        /// <param name="className">Name of the class.</param>
        /// <returns>ClassList.</returns>
        public static ClassList Create(string className = null) => new ClassList(className);

        /// <summary>
        /// Adds the specified class name.
        /// </summary>
        /// <param name="className">Name of the class.</param>
        /// <param name="condition">if set to <c>true</c> [condition].</param>
        /// <returns>ClassList.</returns>
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

        /// <summary>
        /// Adds the specified attributes.
        /// </summary>
        /// <param name="attributes">The attributes.</param>
        /// <returns>ClassList.</returns>
        public ClassList Add(IDictionary<string, object> attributes)
        {
            if (attributes != null && attributes.TryGetValue("class", out var className) && className != null)
            {
                return Add(className.ToString());
            }

            return this;
        }

        /// <summary>
        /// Adds the specified attributes.
        /// </summary>
        /// <param name="attributes">The attributes.</param>
        /// <returns>ClassList.</returns>
        public ClassList Add(IReadOnlyDictionary<string, object> attributes)
        {
            if (attributes != null && attributes.TryGetValue("class", out var className) && className != null)
            {
                return Add(className.ToString());
            }

            return this;
        }

        /// <summary>
        /// Adds the specified field.
        /// </summary>
        /// <param name="field">The field.</param>
        /// <param name="context">The context.</param>
        /// <returns>ClassList.</returns>
        public ClassList Add(FieldIdentifier field, EditContext context)
        {
            if (field.FieldName != null && context != null)
            {
                return Add(context.FieldCssClass(field));
            }

            return this;
        }

        /// <summary>
        /// Adds the disabled.
        /// </summary>
        /// <param name="condition">if set to <c>true</c> [condition].</param>
        /// <returns>ClassList.</returns>
        public ClassList AddDisabled(bool condition = true)
        {
            return Add("rz-state-disabled", condition);
        }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>A <see cref="System.String" /> that represents this instance.</returns>
        public override string ToString()
        {
            return builder.ToString();
        }
    }
}