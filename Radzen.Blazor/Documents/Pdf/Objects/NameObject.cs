using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace Radzen.Documents.Pdf.Objects;

// ISO 32000-1 7.3.5: a name is written with a leading slash; delimiters, whitespace,
// the number sign and bytes outside 0x21-0x7E are escaped as #xx in uppercase hex.
internal sealed class NameObject(string value) : DocumentObject
{
    public string Value { get; } = value;

    internal override void Write(Stream stream, WriteContext context)
    {
        WriteEscaped(stream, Value);
    }

    internal static void WriteEscaped(Stream stream, string name)
    {
        if (NeedsEscaping(name))
        {
            PdfBytes.WriteAscii(stream, Escape(name));
            return;
        }

        stream.WriteByte((byte)'/');
        PdfBytes.WriteAscii(stream, name);
    }

    internal static bool NeedsEscaping(string name)
    {
        foreach (var ch in name)
        {
            ThrowIfUnencodable(ch, name);

            var code = ch & 0xFF;
            if (code <= 0x20 || code >= 0x7F || Lexer.IsDelimiter((byte)code) || code == '#')
            {
                return true;
            }
        }

        return false;
    }

    internal static string Escape(string name)
    {
        var builder = new StringBuilder(name.Length + 1);
        builder.Append('/');

        foreach (var ch in name)
        {
            ThrowIfUnencodable(ch, name);

            var code = ch & 0xFF;
            if (code > 0x20 && code < 0x7F && !Lexer.IsDelimiter((byte)code) && code != '#')
            {
                builder.Append((char)code);
            }
            else
            {
                builder.Append('#').Append(code.ToString("X2", CultureInfo.InvariantCulture));
            }
        }

        return builder.ToString();
    }

    private static void ThrowIfUnencodable(char ch, string name)
    {
        if (ch > 0xFF)
        {
            throw new NotSupportedException($"Name '{name}' contains a code point (U+{(int)ch:X4}) outside the encodable range.");
        }
    }
}
