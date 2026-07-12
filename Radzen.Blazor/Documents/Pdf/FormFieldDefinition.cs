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
