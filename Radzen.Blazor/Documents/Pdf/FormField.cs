using Radzen.Documents.Pdf.Objects;
using System;
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

    internal FormField(DocumentReader reader, DictionaryObject dictionary, string name)
    {
        this.reader = reader;
        Dictionary = dictionary;
        Name = name;
    }

    internal DictionaryObject Dictionary { get; }

    /// <summary>
    /// Gets the fully qualified field name (ancestor <c>/T</c> entries joined with
    /// <c>.</c>). This is the name <see cref="AcroForm.FillField"/> and the other
    /// <see cref="AcroForm"/> lookups accept, and it appears in
    /// <see cref="AcroForm.FieldNames"/>.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the field's own partial name: its <c>/T</c> entry alone, without any
    /// ancestor qualification. Not unique across a hierarchical form; use
    /// <see cref="Name"/> to address the field.
    /// </summary>
    public string PartialName
        => reader.GetString(Dictionary, "T") is { } text
            ? DecodeTextString(text)
            : string.Empty;

    /// <summary>
    /// Gets the field value from its <c>/V</c> entry: the text for a text field,
    /// the selected state name for a button, or the selected item(s) for a choice
    /// field. A field with no own <c>/V</c> inherits it from an ancestor field.
    /// </summary>
    public string? Value => ValueText(reader, InheritedEntry("V"));

    // An annotation is a form widget iff its /Subtype is /Widget (ISO 32000-1 12.5.6.19).
    internal static bool IsWidget(DocumentReader reader, DictionaryObject annotation)
        => string.Equals(reader.GetName(annotation, "Subtype"), "Widget", StringComparison.Ordinal);

    internal static string? ValueText(DocumentReader reader, DocumentObject? value)
        => value switch
        {
            StringObject text => DecodeTextString(text.Value),
            NameObject name => name.Value,
            ArrayObject items => JoinValues(reader, items),
            _ => null,
        };

    private DocumentObject? InheritedEntry(string key) => InheritedAttribute(reader, Dictionary, key);

    internal static IEnumerable<DictionaryObject> ParentChain(DocumentReader reader, DictionaryObject dictionary)
    {
        var seen = new HashSet<DictionaryObject>(ReferenceEqualityComparer.Instance);
        for (var current = dictionary; current is not null; current = reader.GetDictionary(current, "Parent"))
        {
            if (!seen.Add(current))
            {
                throw new DocumentParseException("Cyclic /Parent reference in the field tree.");
            }

            yield return current;
        }
    }

    // /V, /FT, /Ff and /Q may be inherited from a non-terminal parent on the /Parent chain (ISO 32000 12.7.3.1).
    internal static DocumentObject? InheritedAttribute(DocumentReader reader, DictionaryObject dictionary, string key)
    {
        foreach (var current in ParentChain(reader, dictionary))
        {
            if (current.TryGetValue(key, out var value))
            {
                return reader.Resolve(value!);
            }
        }

        return null;
    }

    // A multi-select choice field's /V is an array of the selected items (ISO 32000-1 12.7.4.4).
    private static string JoinValues(DocumentReader reader, ArrayObject items)
    {
        var parts = new List<string>();
        foreach (var item in items)
        {
            if (reader.AsString(item) is { } text)
            {
                parts.Add(DecodeTextString(text));
            }
        }

        return string.Join(", ", parts);
    }

    private static readonly Encoding StrictUtf8 = new UTF8Encoding(false, throwOnInvalidBytes: true);

    // A PDF text string (ISO 32000 7.9.2.2) is UTF-16BE when prefixed FE FF, UTF-8 when prefixed EF BB BF (ISO 32000-2), otherwise PDFDocEncoding.
    internal static string DecodeTextString(string raw)
    {
        if (raw.Length >= 2 && raw[0] == 0xFE && raw[1] == 0xFF)
        {
            return Encoding.BigEndianUnicode.GetString(ToBytes(raw, 2));
        }

        if (raw.Length >= 3 && raw[0] == 0xEF && raw[1] == 0xBB && raw[2] == 0xBF)
        {
            try
            {
                return StrictUtf8.GetString(ToBytes(raw, 3));
            }
            catch (DecoderFallbackException)
            {
                return raw;
            }
        }

        var decoded = new char[raw.Length];
        for (var i = 0; i < raw.Length; i++)
        {
            var ch = raw[i];
            decoded[i] = ch <= 0xFF ? PdfDocEncoding.ToUnicode[ch] : ch;
        }

        return new string(decoded);
    }

    private static byte[] ToBytes(string raw, int start)
    {
        var bytes = new byte[raw.Length - start];
        for (var i = 0; i < bytes.Length; i++)
        {
            bytes[i] = (byte)raw[i + start];
        }

        return bytes;
    }

    /// <summary>
    /// Gets the field type from its <c>/FT</c> entry. A field with no own <c>/FT</c>
    /// inherits it from an ancestor field.
    /// </summary>
    public FormFieldType Type
        => InheritedEntry("FT") is NameObject name
            ? name.Value switch
            {
                "Btn" => FormFieldType.Button,
                "Ch" => FormFieldType.Choice,
                "Sig" => FormFieldType.Signature,
                _ => FormFieldType.Text,
            }
            : FormFieldType.Text;
}
