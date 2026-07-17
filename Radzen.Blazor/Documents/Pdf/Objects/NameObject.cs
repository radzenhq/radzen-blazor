using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace Radzen.Documents.Pdf.Objects;

/// <summary>
/// A PDF name object (ISO 32000-1 section 7.3.5). Serialized with a leading
/// slash; delimiters, whitespace, the number sign, and bytes outside the
/// range 0x21-0x7E are escaped as <c>#xx</c> using uppercase hexadecimal.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="NameObject"/> class.
/// </remarks>
/// <param name="value">The unescaped name (without the leading slash).</param>
public sealed class NameObject(string value) : DocumentObject
{

    /// <summary>
    /// Gets the unescaped name value.
    /// </summary>
    public string Value { get; } = value;

    internal override void Write(Stream stream, WriteContext context)
    {
        WriteEscaped(stream, Value);
    }

    // Streams the escaped form without allocating when no byte needs escaping,
    // which is the case for almost every name written by the generator.
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

    // Whether any character forces the #xx-escaped form. Returning early on the first such
    // character is safe because Escape re-scans and re-applies ThrowIfUnencodable to the rest.
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

    // Names are byte sequences; a code point above Latin-1 cannot be represented without
    // silently aliasing to a different name (e.g. U+0141 -> 'A'), so fail loud instead.
    private static void ThrowIfUnencodable(char ch, string name)
    {
        if (ch > 0xFF)
        {
            throw new NotSupportedException($"Name '{name}' contains a code point (U+{(int)ch:X4}) outside the encodable range.");
        }
    }
}
