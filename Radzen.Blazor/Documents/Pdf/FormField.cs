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
    public string? Value
        => InheritedEntry("V") switch
        {
            StringObject text => DecodeTextString(text.Value),
            NameObject name => name.Value,
            ArrayObject items => JoinValues(items),
            _ => null,
        };

    private DocumentObject? InheritedEntry(string key) => InheritedAttribute(reader, Dictionary, key);

    // Walks a field/widget's /Parent chain for the nearest inheritable attribute (ISO 32000
    // 12.7.3.1) and returns it resolved: /V, /FT, /Ff and /Q can all be set on a non-terminal
    // parent and inherited by its kids. The walk is bounded against a cyclic /Parent chain.
    internal static DocumentObject? InheritedAttribute(DocumentReader reader, DictionaryObject dictionary, string key)
    {
        var current = dictionary;
        for (var depth = 0; current is not null && depth < 32; depth++)
        {
            if (current.TryGetValue(key, out var value))
            {
                return reader.Resolve(value!);
            }

            current = reader.GetDictionary(current, "Parent");
        }

        return null;
    }

    private string JoinValues(ArrayObject items)
    {
        var parts = new List<string>();
        foreach (var item in items)
        {
            if (reader.AsString(item) is { } text)
            {
                parts.Add(DecodeTextString(text));
            }
        }

        return string.Join("\n", parts);
    }

    private static readonly Encoding StrictUtf8 = new UTF8Encoding(false, throwOnInvalidBytes: true);

    // A PDF text string (ISO 32000 7.9.2.2) is UTF-16BE when prefixed FE FF, or UTF-8
    // when prefixed EF BB BF (ISO 32000-2); otherwise it is PDFDocEncoding/Latin1, which
    // StringObject.Value already exposes verbatim as chars 0-255. Both prefixes are
    // themselves PDFDocEncodable ("þÿ", "ï»¿") and the spec resolves that by fiat: the
    // prefix wins. The strict-UTF8 fallback only covers the residual case where the
    // prefix is real Latin1 text, which a malformed UTF-8 remainder reveals.
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

        return raw;
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
