using Radzen.Documents.Pdf.Objects;

namespace Radzen.Documents.Pdf;


/// <summary>
/// A single terminal AcroForm field backed by its live widget dictionary.
/// Mutations applied through <see cref="AcroForm"/> are written back to this
/// dictionary and survive the next save.
/// </summary>
public sealed class FormField
{
    private readonly DocumentReader reader;

    internal FormField(DocumentReader reader, DictionaryObject dictionary)
    {
        this.reader = reader;
        Dictionary = dictionary;
    }

    internal DictionaryObject Dictionary { get; }

    /// <summary>Gets the fully qualified field name from its <c>/T</c> entry.</summary>
    public string Name
        => Dictionary.TryGetValue("T", out var value) && reader.Resolve(value!) is StringObject text
            ? text.Value
            : string.Empty;

    /// <summary>
    /// Gets the field value from its <c>/V</c> entry: the text for a text field,
    /// or the selected state name for a button or choice field.
    /// </summary>
    public string? Value
    {
        get
        {
            if (!Dictionary.TryGetValue("V", out var value))
            {
                return null;
            }

            return reader.Resolve(value!) switch
            {
                StringObject text => text.Value,
                NameObject name => name.Value,
                _ => null,
            };
        }
    }

    /// <summary>Gets the field type from its <c>/FT</c> entry.</summary>
    public FormFieldType Type
        => Dictionary.TryGetValue("FT", out var value) && reader.Resolve(value!) is NameObject name
            ? name.Value switch
            {
                "Btn" => FormFieldType.Button,
                "Ch" => FormFieldType.Choice,
                "Sig" => FormFieldType.Signature,
                _ => FormFieldType.Text,
            }
            : FormFieldType.Text;
}
