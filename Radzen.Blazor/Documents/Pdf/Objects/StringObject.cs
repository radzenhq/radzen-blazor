using System;
using System.IO;
using System.Text;

namespace Radzen.Documents.Pdf.Objects;

/// <summary>
/// A PDF literal string object (ISO 32000-1 section 7.3.4.2). Serialized in
/// parentheses; the backslash and both parentheses are escaped, the named
/// control escapes are used where applicable, and any other byte outside
/// printable ASCII is written as a 3-digit octal escape. A value containing
/// characters above U+00FF is written as a UTF-16BE text string with a byte
/// order mark (ISO 32000-2 section 7.9.2.2).
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="StringObject"/> class.
/// </remarks>
/// <param name="value">The raw string value; each character is treated as a byte 0-255.</param>
public sealed class StringObject(string value) : DocumentObject
{

    /// <summary>
    /// Gets the raw string value.
    /// </summary>
    public string Value { get; } = value;

    internal override void Write(Stream stream, WriteContext context)
    {
        var bytes = EncodeBytes(Value);
        var encryptor = context.Encryptor;
        if (encryptor is not null)
        {
            bytes = encryptor.EncryptString(bytes, context.ObjectNumber, context.Generation);
        }

        var builder = new StringBuilder(bytes.Length + 2);
        PdfLiteralString.AppendEscaped(builder, bytes, binary: false);
        PdfBytes.WriteAscii(stream, builder.ToString());
    }

    internal static StringObject FromText(string value)
    {
        foreach (var ch in value)
        {
            if (ch > 0xFF || PdfDocEncoding.IsRemapped(ch))
            {
                return new StringObject(Encoding.Latin1.GetString(Utf16WithBom(value)));
            }
        }

        return new StringObject(value);
    }

    private static byte[] EncodeBytes(string value)
    {
        foreach (var ch in value)
        {
            if (ch > 0xFF)
            {
                return Utf16WithBom(value);
            }
        }

        var raw = new byte[value.Length];
        for (var i = 0; i < value.Length; i++)
        {
            raw[i] = (byte)value[i];
        }

        return raw;
    }

    private static byte[] Utf16WithBom(string value)
    {
        var bytes = new byte[2 + Encoding.BigEndianUnicode.GetByteCount(value)];
        bytes[0] = 0xFE;
        bytes[1] = 0xFF;
        Encoding.BigEndianUnicode.GetBytes(value, 0, value.Length, bytes, 2);
        return bytes;
    }

}

internal static class PdfLiteralString
{
    // ISO 32000-1 7.3.4.2: a literal string is parenthesised; the backslash and unbalanced
    // parentheses are escaped, the named control escapes and three-digit octal escapes are
    // permitted, and any other byte may be written raw.
    public static void AppendEscaped(StringBuilder builder, ReadOnlySpan<byte> bytes, bool binary)
    {
        builder.Append('(');
        foreach (var b in bytes)
        {
            switch (b)
            {
                case (byte)'\\':
                    builder.Append("\\\\");
                    break;
                case (byte)'(':
                    builder.Append("\\(");
                    break;
                case (byte)')':
                    builder.Append("\\)");
                    break;
                case (byte)'\n' when !binary:
                    builder.Append("\\n");
                    break;
                case (byte)'\r' when !binary:
                    builder.Append("\\r");
                    break;
                case (byte)'\t' when !binary:
                    builder.Append("\\t");
                    break;
                case (byte)'\b' when !binary:
                    builder.Append("\\b");
                    break;
                case (byte)'\f' when !binary:
                    builder.Append("\\f");
                    break;
                default:
                    if ((b >= 0x20 && b <= 0x7E) || (binary && b >= 0x80))
                    {
                        builder.Append((char)b);
                    }
                    else
                    {
                        builder.Append('\\');
                        builder.Append((char)('0' + ((b >> 6) & 0x7)));
                        builder.Append((char)('0' + ((b >> 3) & 0x7)));
                        builder.Append((char)('0' + (b & 0x7)));
                    }

                    break;
            }
        }

        builder.Append(')');
    }
}
