using System;
using System.Collections.Generic;
using Radzen.Documents.Core;

namespace Radzen.Documents;


/// <summary>
/// An interactive form control that flows within a paragraph line, sitting on the line's baseline
/// and advancing it by the control width. The set of control kinds is fixed by this library and
/// cannot be extended from outside it. A form input carries no coordinates and no page index:
/// layout places it like any other inline, and each renderer decides how to express it.
/// </summary>
public abstract class FormInput : Inline
{
    private string name;

    private protected FormInput(string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        this.name = name;
    }

    /// <summary>
    /// Gets or sets the field name. Inputs that share a name are one field: this is how a
    /// <see cref="RadioButton"/> group is expressed, as several placed buttons naming one field.
    /// </summary>
    /// <exception cref="ArgumentException">The value is <see langword="null"/> or empty.</exception>
    public string Name
    {
        get => name;
        set
        {
            ArgumentException.ThrowIfNullOrEmpty(value);
            name = value;
        }
    }

    /// <summary>Gets or sets whether the field must be filled in before the form is submitted.</summary>
    public bool Required { get; set; }

    /// <summary>
    /// Gets or sets the caption naming this field for a reader who cannot see the surrounding
    /// text, or <see langword="null"/> (the default) when the field declares none. It is the
    /// field's accessible name: the PDF renderer writes it as the widget's alternate field name
    /// and as the alternate description of the Form structure element wrapping the widget.
    /// </summary>
    public string? Label { get; set; }
}


/// <summary>A single-line text entry field, as wide as <see cref="Width"/> and as tall as its font.</summary>
public sealed class TextInput : FormInput
{
    private Unit? width;

    /// <summary>Initializes a text input with the specified field name.</summary>
    /// <param name="name">The field name.</param>
    /// <exception cref="ArgumentException"><paramref name="name"/> is <see langword="null"/> or empty.</exception>
    public TextInput(string name) : base(name)
    {
    }

    /// <summary>Gets or sets the initial value. Defaults to the empty string.</summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the drawn width, or <see langword="null"/> (the default) for ten times the
    /// font size.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is relative or is not greater than zero.</exception>
    public Unit? Width
    {
        get => width;
        set => width = AuthoredNumber.AbsolutePositive(value, "TextInput.Width");
    }
}


/// <summary>A check box sized from its font, checked or clear.</summary>
public sealed class CheckBox : FormInput
{
    /// <summary>Initializes a check box with the specified field name.</summary>
    /// <param name="name">The field name.</param>
    /// <exception cref="ArgumentException"><paramref name="name"/> is <see langword="null"/> or empty.</exception>
    public CheckBox(string name) : base(name)
    {
    }

    /// <summary>Gets or sets whether the box is initially checked.</summary>
    public bool Checked { get; set; }
}


/// <summary>
/// One button of a radio group, sized from its font. Every button naming the same field belongs to
/// the same group; the buttons of a group must carry distinct <see cref="Value"/>s, and at most one
/// of them may be <see cref="Selected"/>.
/// </summary>
public sealed class RadioButton : FormInput
{
    private string value;

    /// <summary>Initializes a radio button with the specified field name and export value.</summary>
    /// <param name="name">The field name shared by every button of the group.</param>
    /// <param name="value">The value the group exports when this button is chosen.</param>
    /// <exception cref="ArgumentException"><paramref name="name"/> or <paramref name="value"/> is
    /// <see langword="null"/> or empty.</exception>
    public RadioButton(string name, string value) : base(name)
    {
        this.value = Validated(value);
    }

    /// <summary>Gets or sets the value the group exports when this button is chosen.</summary>
    /// <exception cref="ArgumentException">The value is <see langword="null"/> or empty.</exception>
    public string Value
    {
        get => value;
        set => this.value = Validated(value);
    }

    /// <summary>Gets or sets whether this button is the initially chosen one of its group.</summary>
    public bool Selected { get; set; }

    private static string Validated(string value)
    {
        ArgumentException.ThrowIfNullOrEmpty(value);
        return value;
    }
}


/// <summary>A drop-down list of options, as wide as <see cref="Width"/> and as tall as its font.</summary>
public sealed class DropDown : FormInput
{
    private Unit? width;

    /// <summary>Initializes a drop-down with the specified field name.</summary>
    /// <param name="name">The field name.</param>
    /// <exception cref="ArgumentException"><paramref name="name"/> is <see langword="null"/> or empty.</exception>
    public DropDown(string name) : base(name)
    {
    }

    /// <summary>Gets the selectable options, in the order they are offered.</summary>
    public IList<string> Options { get; } = [];

    /// <summary>Gets or sets the initially selected option. Defaults to the empty string.</summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the drawn width, or <see langword="null"/> (the default) for ten times the
    /// font size.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is relative or is not greater than zero.</exception>
    public Unit? Width
    {
        get => width;
        set => width = AuthoredNumber.AbsolutePositive(value, "DropDown.Width");
    }
}
