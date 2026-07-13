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
        builder.Append('(');

        foreach (var b in bytes)
        {
            var code = (int)b;
            switch (code)
            {
                case '\\':
                    builder.Append("\\\\");
                    break;
                case '(':
                    builder.Append("\\(");
                    break;
                case ')':
                    builder.Append("\\)");
                    break;
                case '\n':
                    builder.Append("\\n");
                    break;
                case '\r':
                    builder.Append("\\r");
                    break;
                case '\t':
                    builder.Append("\\t");
                    break;
                case '\b':
                    builder.Append("\\b");
                    break;
                case '\f':
                    builder.Append("\\f");
                    break;
                default:
                    if (code >= 0x20 && code <= 0x7E)
                    {
                        builder.Append((char)code);
                    }
                    else
                    {
                        builder.Append('\\').Append(ToOctal(code));
                    }

                    break;
            }
        }

        builder.Append(')');
        PdfBytes.WriteAscii(stream, builder.ToString());
    }

    private static byte[] EncodeBytes(string value)
    {
        foreach (var ch in value)
        {
            if (ch > 0xFF)
            {
                var bytes = new byte[2 + Encoding.BigEndianUnicode.GetByteCount(value)];
                bytes[0] = 0xFE;
                bytes[1] = 0xFF;
                Encoding.BigEndianUnicode.GetBytes(value, 0, value.Length, bytes, 2);
                return bytes;
            }
        }

        var raw = new byte[value.Length];
        for (var i = 0; i < value.Length; i++)
        {
            raw[i] = (byte)value[i];
        }

        return raw;
    }

    private static string ToOctal(int code)
    {
        var digits = new char[3];
        digits[0] = (char)('0' + ((code >> 6) & 0x7));
        digits[1] = (char)('0' + ((code >> 3) & 0x7));
        digits[2] = (char)('0' + (code & 0x7));
        return new string(digits);
    }
}
