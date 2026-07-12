namespace Radzen.Documents.Pdf;


/// <summary>
/// The base definition of an interactive form field to create on a
/// <see cref="Document"/>. Add definitions to <see cref="Document.FormFields"/>;
/// on save each one becomes a widget annotation on its page and an entry in the
/// catalog <c>/AcroForm /Fields</c> with a generated appearance stream.
/// </summary>
public abstract class FormFieldDefinition
{
    private protected FormFieldDefinition(string name)
    {
        System.ArgumentException.ThrowIfNullOrEmpty(name);
        Name = name;
    }

    /// <summary>Gets the field name (<c>/T</c>).</summary>
    public string Name { get; }

    /// <summary>Gets or sets the zero-based index of the page carrying the field.</summary>
    public int PageIndex { get; set; }

    /// <summary>Gets or sets the left edge of the field rectangle, in PDF user space (origin at the bottom-left of the page).</summary>
    public Unit X { get; set; }

    /// <summary>Gets or sets the bottom edge of the field rectangle, in PDF user space (origin at the bottom-left of the page).</summary>
    public Unit Y { get; set; }

    /// <summary>Gets or sets the width of the field rectangle.</summary>
    public Unit Width { get; set; }

    /// <summary>Gets or sets the height of the field rectangle.</summary>
    public Unit Height { get; set; }
}


/// <summary>
/// A single-line text field (<c>/FT /Tx</c>) with an initial value and the
/// base-14 font and size its <c>/DA</c> declares.
/// </summary>
/// <param name="name">The field name (<c>/T</c>).</param>
public sealed class TextFieldDefinition(string name) : FormFieldDefinition(name)
{
    /// <summary>Gets or sets the initial field value (<c>/V</c>). Defaults to empty.</summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>Gets or sets the font the value renders with. Base-14 families only; defaults to Helvetica 10pt.</summary>
    public Font Font { get; set; } = new();
}


/// <summary>
/// A check box field (<c>/FT /Btn</c>) whose on-state is named <c>Yes</c>.
/// </summary>
/// <param name="name">The field name (<c>/T</c>).</param>
public sealed class CheckBoxFieldDefinition(string name) : FormFieldDefinition(name)
{
    /// <summary>Gets or sets a value indicating whether the box is initially checked.</summary>
    public bool Checked { get; set; }
}


/// <summary>
/// One selectable option of a <see cref="RadioGroupFieldDefinition"/>. Each option
/// becomes a widget annotation (a <c>/Kids</c> entry of the group field) at its own
/// rectangle, with its <see cref="Value"/> used as the widget's on-state name.
/// </summary>
public sealed class RadioOptionDefinition
{
    /// <summary>Initializes a new instance of the <see cref="RadioOptionDefinition"/> class.</summary>
    /// <param name="value">The export value; also the widget's on-state name. Must not be <c>Off</c>.</param>
    public RadioOptionDefinition(string value)
    {
        System.ArgumentException.ThrowIfNullOrEmpty(value);
        if (string.Equals(value, "Off", System.StringComparison.Ordinal))
        {
            throw new System.ArgumentException("A radio option value cannot be 'Off'; that name is reserved for the unselected state.", nameof(value));
        }

        Value = value;
    }

    /// <summary>Gets the export value; also the widget's on-state name.</summary>
    public string Value { get; }

    /// <summary>Gets or sets the left edge of the option rectangle, in PDF user space (origin at the bottom-left of the page).</summary>
    public Unit X { get; set; }

    /// <summary>Gets or sets the bottom edge of the option rectangle, in PDF user space (origin at the bottom-left of the page).</summary>
    public Unit Y { get; set; }

    /// <summary>Gets or sets the width of the option rectangle.</summary>
    public Unit Width { get; set; }

    /// <summary>Gets or sets the height of the option rectangle.</summary>
    public Unit Height { get; set; }
}


/// <summary>
/// A radio button group (<c>/FT /Btn</c> with the Radio flag, <c>/Ff</c> bit 16).
/// The group saves as a parent field holding <c>/V</c> and <c>/DV</c>, with one kid
/// widget annotation per <see cref="Options"/> entry. Option geometry comes from each
/// <see cref="RadioOptionDefinition"/>; the rectangle inherited from
/// <see cref="FormFieldDefinition"/> is ignored. At least two options with distinct
/// values are required, and at most one may be selected.
/// </summary>
/// <param name="name">The field name (<c>/T</c>).</param>
public sealed class RadioGroupFieldDefinition(string name) : FormFieldDefinition(name)
{
    /// <summary>Gets the selectable options. At least two, with distinct values.</summary>
    public System.Collections.Generic.List<RadioOptionDefinition> Options { get; } = [];

    /// <summary>
    /// Gets or sets the value of the initially selected option, or <c>null</c> when
    /// none is selected. Must match the <see cref="RadioOptionDefinition.Value"/> of
    /// one of the <see cref="Options"/>.
    /// </summary>
    public string? SelectedValue { get; set; }
}


/// <summary>
/// A choice field (<c>/FT /Ch</c>) listing <see cref="Options"/> in <c>/Opt</c>.
/// Saves as a combo box (<c>/Ff</c> bit 18) when <see cref="ComboBox"/> is set,
/// otherwise as a list box. The generated appearance shows the selected
/// <see cref="Value"/> as plain text for both variants; the list box does not
/// paint the option list or a selection highlight.
/// </summary>
/// <param name="name">The field name (<c>/T</c>).</param>
public sealed class ChoiceFieldDefinition(string name) : FormFieldDefinition(name)
{
    /// <summary>Gets the options exported to <c>/Opt</c>.</summary>
    public System.Collections.Generic.List<string> Options { get; } = [];

    /// <summary>Gets or sets the selected value (<c>/V</c>). Defaults to empty.</summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>Gets or sets a value indicating whether the field is a combo box rather than a list box.</summary>
    public bool ComboBox { get; set; }

    /// <summary>Gets or sets the font the value renders with. Base-14 families only; defaults to Helvetica 10pt.</summary>
    public Font Font { get; set; } = new();
}
