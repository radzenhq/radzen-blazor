using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Components.Forms;

namespace Radzen.Blazor.Rendering;

/// <summary>
/// Utility for managing a list of CSS classes.
/// </summary>
public readonly struct ClassList
{
    private ClassList(string className = null)
    {
        Add(className);
    }

    private readonly StringBuilder builder = StringBuilderCache.Acquire();

    /// <summary>
    /// Creates the specified class name.
    /// </summary>
    /// <param name="className">Name of the class.</param>
    /// <returns>ClassList.</returns>
    public static ClassList Create(string className = null) => new(className);

    /// <summary>
    /// Adds the specified class name if the condition is true.
    /// </summary>
    /// <param name="className">Name of the class.</param>
    /// <param name="condition">if set to <c>true</c> the class name is added.</param>
    /// <remarks>
    /// The class name is added only if it is not null or empty.
    /// </remarks>
    public ClassList Add(string className, bool condition = true)
    {
        if (condition && !string.IsNullOrWhiteSpace(className))
        {
            if (builder.Length > 0)
            {
                builder.Append(' ');
            }

            builder.Append(className);
        }

        return this;
    }

    /// <summary>
    /// Checks if the provided attributes contain a class name and adds it to the list.
    /// </summary>
    /// <param name="attributes">The attributes.</param>
    public ClassList Add(IDictionary<string, object> attributes)
    {
        if (attributes != null && attributes.TryGetValue("class", out var className) && className != null)
        {
            return Add(className.ToString());
        }

        return this;
    }

    /// <summary>
    /// Checks if the provided attributes contain a class name and adds it to the list.
    /// </summary>
    /// <param name="attributes">The attributes.</param>
    public ClassList Add(IReadOnlyDictionary<string, object> attributes)
    {
        if (attributes != null && attributes.TryGetValue("class", out var className) && className != null)
        {
            return Add(className.ToString());
        }

        return this;
    }

    /// <summary>
    /// Adds the class returned by the EditContext for the specified field identifier
    /// </summary>
    /// <param name="field">The field.</param>
    /// <param name="context">The context.</param>
    public ClassList Add(FieldIdentifier field, EditContext context)
    {
        if (field.FieldName != null && context != null)
        {
            return Add(context.FieldCssClass(field));
        }

        return this;
    }

    /// <summary>
    /// Adds the disabled class if the condition is true.
    /// </summary>
    public ClassList AddDisabled(bool condition = true)
    {
        return Add("rz-state-disabled", condition);
    }

    /// <summary>
    /// Returns a <see cref="System.String" /> that represents this instance.
    /// </summary>
    /// <returns>A <see cref="System.String" /> that represents this instance.</returns>
    public override string ToString() => StringBuilderCache.GetStringAndRelease(builder);
}