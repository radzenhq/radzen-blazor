using System.IO;
using System.Text;

namespace Radzen.Documents.Pdf.Objects;

/// <summary>
/// A PDF literal string object (ISO 32000-1 section 7.3.4.2). Serialized in
/// parentheses; the backslash and both parentheses are escaped, the named
/// control escapes are used where applicable, and any other byte outside
/// printable ASCII is written as a 3-digit octal escape.
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

    /// <inheritdoc />
    public override void Write(Stream stream)
    {
        var builder = new StringBuilder(Value.Length + 2);
        builder.Append('(');

        foreach (var ch in Value)
        {
            var code = ch & 0xFF;
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

    private static string ToOctal(int code)
    {
        var digits = new char[3];
        digits[0] = (char)('0' + ((code >> 6) & 0x7));
        digits[1] = (char)('0' + ((code >> 3) & 0x7));
        digits[2] = (char)('0' + (code & 0x7));
        return new string(digits);
    }
}
