using System;
using System.Collections.Generic;
using Radzen.Documents.Fonts;

namespace Radzen.Documents.Pdf;


/// <summary>
/// The base definition of an interactive form field to create on a
/// <see cref="PortableDocument"/>. Add definitions to <see cref="PortableDocument.FormFields"/>.
/// </summary>
public abstract class FormFieldDefinition
{
    /// <summary>Initializes the shared field state with the given field name.</summary>
    /// <param name="name">The field name; must not be null or empty.</param>
    protected FormFieldDefinition(string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        Name = name;
    }

    /// <summary>Gets the field name.</summary>
    public string Name { get; }

    /// <summary>Gets or sets the zero-based index of the page carrying the field.</summary>
    public int PageIndex { get; set; }

}


/// <summary>Defines a form field represented by one rectangular widget.</summary>
/// <param name="name">The field name; must not be null or empty.</param>
public abstract class PositionedFieldDefinition(string name) : FormFieldDefinition(name)
{
    /// <summary>Gets or sets the left edge of the field rectangle, in PDF user space.</summary>
    public Unit X { get; set; }

    /// <summary>Gets or sets the bottom edge of the field rectangle, in PDF user space.</summary>
    public Unit Y { get; set; }

    /// <summary>Gets or sets the width of the field rectangle.</summary>
    public Unit Width { get; set; }

    /// <summary>Gets or sets the height of the field rectangle.</summary>
    public Unit Height { get; set; }
}


/// <summary>A single-line text field with an initial value and font.</summary>
/// <param name="name">The field name.</param>
public sealed class TextFieldDefinition(string name) : PositionedFieldDefinition(name)
{
    /// <summary>Gets or sets the initial field value. Defaults to empty.</summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>Gets or sets the font the value renders with. Defaults to Helvetica 10pt.</summary>
    public Font Font { get; set; } = new();

    /// <summary>Gets or sets a value indicating whether the field accepts multiple lines of text.</summary>
    public bool Multiline { get; set; }

    /// <summary>Gets or sets a value indicating whether the field masks its input.</summary>
    public bool Password { get; set; }

    /// <summary>Gets or sets a value indicating whether the value uses equally spaced comb cells.</summary>
    public bool Comb { get; set; }

    /// <summary>Gets or sets the maximum accepted character count, or <c>null</c> for no limit.</summary>
    public int? MaxLength { get; set; }

}


/// <summary>A check box field whose on-state is named <c>Yes</c>.</summary>
/// <param name="name">The field name.</param>
public sealed class CheckBoxFieldDefinition(string name) : PositionedFieldDefinition(name)
{
    /// <summary>Gets or sets a value indicating whether the box is initially checked.</summary>
    public bool Checked { get; set; }
}


/// <summary>One selectable option of a <see cref="RadioGroupFieldDefinition"/>.</summary>
public sealed class RadioOptionDefinition
{
    /// <summary>Initializes a radio option with the given export value.</summary>
    /// <param name="value">The export value; must not be <c>Off</c>.</param>
    public RadioOptionDefinition(string value)
    {
        ArgumentException.ThrowIfNullOrEmpty(value);
        if (string.Equals(value, "Off", StringComparison.Ordinal))
        {
            throw new ArgumentException("A radio option value cannot be 'Off'; that name is reserved for the unselected state.", nameof(value));
        }

        Value = value;
    }

    /// <summary>Gets the export value.</summary>
    public string Value { get; }

    /// <summary>Gets or sets the left edge of the option rectangle, in PDF user space.</summary>
    public Unit X { get; set; }

    /// <summary>Gets or sets the bottom edge of the option rectangle, in PDF user space.</summary>
    public Unit Y { get; set; }

    /// <summary>Gets or sets the width of the option rectangle.</summary>
    public Unit Width { get; set; }

    /// <summary>Gets or sets the height of the option rectangle.</summary>
    public Unit Height { get; set; }

}


/// <summary>A radio button group whose options each define their own rectangle.</summary>
/// <param name="name">The field name.</param>
public sealed class RadioGroupFieldDefinition(string name) : FormFieldDefinition(name)
{
    /// <summary>Gets the selectable options. At least two, with distinct values.</summary>
    public List<RadioOptionDefinition> Options { get; } = [];

    /// <summary>Gets or sets the initially selected option value, or <c>null</c>.</summary>
    public string? SelectedValue { get; set; }

}


/// <summary>A choice field listing selectable options.</summary>
/// <param name="name">The field name.</param>
public sealed class ChoiceFieldDefinition(string name) : PositionedFieldDefinition(name)
{
    /// <summary>Gets the options exported by the field.</summary>
    public List<string> Options { get; } = [];

    /// <summary>Gets or sets the selected value. Defaults to empty.</summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>Gets or sets a value indicating whether the field is a combo box.</summary>
    public bool ComboBox { get; set; }

    /// <summary>Gets or sets the font the value renders with. Defaults to Helvetica 10pt.</summary>
    public Font Font { get; set; } = new();

}
