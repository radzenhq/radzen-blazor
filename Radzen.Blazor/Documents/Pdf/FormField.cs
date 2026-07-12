using Radzen.Documents.Pdf.Objects;
using System.Collections.Generic;
using System.Text;

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
            ? DecodeTextString(text.Value)
            : string.Empty;

    /// <summary>
    /// Gets the field value from its <c>/V</c> entry: the text for a text field,
    /// the selected state name for a button, or the selected item(s) for a choice
    /// field. A field with no own <c>/V</c> inherits it from an ancestor field.
    /// </summary>
    public string? Value
    {
        get
        {
            var value = InheritedValue();
            if (value is null)
            {
                return null;
            }

            return reader.Resolve(value) switch
            {
                StringObject text => DecodeTextString(text.Value),
                NameObject name => name.Value,
                ArrayObject items => JoinValues(items),
                _ => null,
            };
        }
    }

    // Walks the /Parent chain for the nearest /V, since a choice or text value can be
    // set on a non-terminal parent and inherited by its widget kids (ISO 32000 12.7.3.1).
    private DocumentObject? InheritedValue()
    {
        var current = Dictionary;
        for (var depth = 0; current is not null && depth < 32; depth++)
        {
            if (current.TryGetValue("V", out var value))
            {
                return value;
            }

            current = current.TryGetValue("Parent", out var parent)
                && reader.Resolve(parent!) is DictionaryObject next
                ? next
                : null;
        }

        return null;
    }

    private string JoinValues(ArrayObject items)
    {
        var parts = new List<string>();
        foreach (var item in items)
        {
            if (reader.Resolve(item) is StringObject text)
            {
                parts.Add(DecodeTextString(text.Value));
            }
        }

        return string.Join("\n", parts);
    }

    // A PDF text string (ISO 32000 7.9.2.2) whose raw bytes start with the FE FF byte
    // order mark is UTF-16BE; otherwise the bytes are PDFDocEncoding/Latin1, which
    // StringObject.Value already exposes verbatim as chars 0-255.
    internal static string DecodeTextString(string raw)
    {
        if (raw.Length < 2 || raw[0] != 0xFE || raw[1] != 0xFF)
        {
            return raw;
        }

        var bytes = new byte[raw.Length - 2];
        for (var i = 0; i < bytes.Length; i++)
        {
            bytes[i] = (byte)raw[i + 2];
        }

        return Encoding.BigEndianUnicode.GetString(bytes);
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
