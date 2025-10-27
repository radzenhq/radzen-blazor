using System;
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
    /// Adds the specified class name if the condition is true. The class name is added only if it is not null or empty.
    /// </summary>
    /// <param name="className">Name of the class.</param>
    /// <param name="condition">if set to <c>true</c> the class name is added.</param>
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
        if (attributes != null && attributes.TryGetValue("class", out var value) && value is string @class)
        {
            return Add(@class);
        }

        return this;
    }

    /// <summary>
    /// Checks if the provided attributes contain a class name and adds it to the list.
    /// </summary>
    /// <param name="attributes">The attributes.</param>
    public ClassList Add(IReadOnlyDictionary<string, object> attributes)
    {
        if (attributes != null && attributes.TryGetValue("class", out var value) && value is string @class)
        {
            return Add(@class);
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
    public ClassList AddDisabled(bool condition = true) => Add("rz-state-disabled", condition);

    /// <summary>
    /// Adds the specified size as button size class.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public ClassList AddButtonSize(ButtonSize size) => size switch {
        ButtonSize.Small => Add("rz-button-sm"),
        ButtonSize.Large => Add("rz-button-lg"),
        ButtonSize.Medium => Add("rz-button-md"),
        ButtonSize.ExtraSmall => Add("rz-button-xs"),
        _ => throw new ArgumentOutOfRangeException(nameof(size), size, null)
    };

    /// <summary>
    /// Adds the specified variant as variant CSS class.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public ClassList AddVariant(Variant variant) => variant switch {
        Variant.Filled => Add("rz-variant-filled"),
        Variant.Flat => Add("rz-variant-flat"),
        Variant.Outlined => Add("rz-variant-outlined"),
        Variant.Text => Add("rz-variant-text"),
        _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, null)
    };

    /// <summary>
    ///  Adds the specified button style as shade CSS class.
    /// </summary>
    /// <param name="style"></param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public ClassList AddButtonStyle(ButtonStyle style) => style switch {
        ButtonStyle.Primary => Add("rz-primary"),
        ButtonStyle.Secondary => Add("rz-secondary"),
        ButtonStyle.Light => Add("rz-light"),
        ButtonStyle.Base => Add("rz-base"),
        ButtonStyle.Dark => Add("rz-dark"),
        ButtonStyle.Success => Add("rz-success"),
        ButtonStyle.Warning => Add("rz-warning"),
        ButtonStyle.Danger => Add("rz-danger"),
        ButtonStyle.Info => Add("rz-info"),
        _ => throw new ArgumentOutOfRangeException(nameof(style), style, null)
    };

    /// <summary>
    /// Adds the specified shade as a CSS class.
    /// </summary>
    /// <param name="shade"></param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public ClassList AddShade(Shade shade) => shade switch {
        Shade.Default => Add("rz-shade-default"),
        Shade.Light => Add("rz-shade-light"),
        Shade.Dark => Add("rz-shade-dark"),
        Shade.Lighter => Add("rz-shade-lighter"),
        Shade.Darker => Add("rz-shade-darker"),
        _ => throw new ArgumentOutOfRangeException(nameof(shade), shade, null)
    };

    /// <summary>
    /// Adds the specified horizontal alignment as a CSS class.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public ClassList AddHorizontalAlign(HorizontalAlign alignment) => alignment switch {
        HorizontalAlign.Center => Add("rz-align-center"),
        HorizontalAlign.Left => Add("rz-align-left"),
        HorizontalAlign.Right => Add("rz-align-right"),
        HorizontalAlign.Justify => Add("rz-align-justify"),
        _ => throw new ArgumentOutOfRangeException(nameof(alignment), alignment, null)
    };

    /// <summary>
    /// Returns a <see cref="System.String" /> that represents this instance.
    /// </summary>
    /// <returns>A <see cref="System.String" /> that represents this instance.</returns>
    public override string ToString() => StringBuilderCache.GetStringAndRelease(builder);
}