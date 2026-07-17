using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Radzen.Documents.Pdf.Objects;

internal sealed class Lexer(byte[] data, int position)
{
    private const int MaxInternedLength = 16;

    private static readonly string[] InternedTokens =
    [
        // Keywords.
        "obj", "endobj", "stream", "endstream", "R", "true", "false", "null",
        "xref", "trailer", "startxref",

        // Structure.
        "Type", "Subtype", "Length", "Length1", "Filter", "DecodeParms", "Root", "Info",
        "Size", "Prev", "First", "Extends", "N", "W", "Index", "ID", "Encrypt",
        "Catalog", "Pages", "Page", "Kids", "Count", "Parent", "ObjStm", "XRef",

        // Page and resources.
        "MediaBox", "CropBox", "Resources", "Contents", "Rotate", "Annots", "Group",
        "Font", "XObject", "ExtGState", "ColorSpace", "Pattern", "Shading", "Properties",
        "ProcSet", "Image", "Form", "Width", "Height",

        // Fonts.
        "BaseFont", "Encoding", "FirstChar", "LastChar", "Widths", "FontDescriptor",
        "ToUnicode", "DescendantFonts", "CIDToGIDMap", "TrueType", "Type0", "Type1",
        "Type1C", "FontFile", "FontFile2", "FontFile3", "Differences",

        // Filters.
        "FlateDecode", "LZWDecode", "ASCII85Decode", "ASCIIHexDecode", "RunLengthDecode",
        "DCTDecode", "JPXDecode", "CCITTFaxDecode", "Crypt",

        // Common values.
        "DeviceRGB", "DeviceGray", "DeviceCMYK", "Indexed", "ICCBased", "Separation",
        "Predictor", "Columns", "Colors", "BitsPerComponent", "BitsPerCoordinate",
    ];

    // Bucketed by length and first byte so a lookup compares against one or two candidates.
    private static readonly Dictionary<int, string[]> Interned = BuildInterned();

    private readonly byte[] data = data;
    private int position = position;

    public int Position => position;

    public static bool IsWhitespace(byte b)
        => b is 0 or 9 or 10 or 12 or 13 or 32;

    public static bool IsDelimiter(byte b)
        => b is (byte)'(' or (byte)')' or (byte)'<' or (byte)'>'
            or (byte)'[' or (byte)']' or (byte)'{' or (byte)'}'
            or (byte)'/' or (byte)'%';

    // What counts as a number, a string or a hex string is one grammar (ISO 32000-1 7.3);
    // only what to do with malformed input differs. A file object must be rejected so a
    // corrupt document cannot be silently misread, while a content stream must recover and
    // keep rendering, so recovery is a parameter of the shared readers below - never a
    // second grammar.
    public enum Recovery
    {
        Strict,
        Lenient,
    }

    // Whether a hex string's closing '>' is required is a separate axis from Recovery: the
    // 7.3.4.3 string object needs it as a delimiter, while for the 7.4.2 ASCIIHexDecode
    // filter the stream /Length already bounds the data, so running out is legal there even
    // though a bad digit is still fatal.
    public enum HexEnd
    {
        Required,
        Optional,
    }

    public static bool IsNumberStart(byte b)
        => b is (byte)'+' or (byte)'-' or (byte)'.' or (>= (byte)'0' and <= (byte)'9');

    public Token Next()
    {
        SkipWhitespaceAndComments();
        if (position >= data.Length)
        {
            return Token.Delimiter(TokenKind.EndOfData);
        }

        var b = data[position];
        switch (b)
        {
            case (byte)'[':
                position++;
                return Token.Delimiter(TokenKind.ArrayOpen);
            case (byte)']':
                position++;
                return Token.Delimiter(TokenKind.ArrayClose);
            case (byte)'<':
                if (position + 1 < data.Length && data[position + 1] == (byte)'<')
                {
                    position += 2;
                    return Token.Delimiter(TokenKind.DictOpen);
                }

                return ReadHexString();
            case (byte)'>':
                if (position + 1 < data.Length && data[position + 1] == (byte)'>')
                {
                    position += 2;
                    return Token.Delimiter(TokenKind.DictClose);
                }

                throw new DocumentParseException("Unexpected '>'.", position);
            case (byte)'(':
                return ReadLiteralString();
            case (byte)'/':
                return ReadName();
            default:
                if (IsNumberStart(b))
                {
                    return ReadNumber();
                }

                return ReadKeyword();
        }
    }

    private void SkipWhitespaceAndComments()
    {
        while (position < data.Length)
        {
            var b = data[position];
            if (IsWhitespace(b))
            {
                position++;
            }
            else if (b == (byte)'%')
            {
                while (position < data.Length && data[position] != 10 && data[position] != 13)
                {
                    position++;
                }
            }
            else
            {
                break;
            }
        }
    }

    private Token ReadNumber() => ReadNumber(data, ref position, Recovery.Strict)!.Value;

    // ISO 32000-1 7.3.3: a number is an optional sign, digits and at most one decimal point.
    // There is no exponent notation in PDF, so "1e3" is malformed input in every context and
    // must never parse as 1000. A numeric object runs to the next whitespace or delimiter, so
    // the whole run is validated rather than stopping at the first byte that does not fit.
    // Lenient callers get null for a malformed run and skip it; strict callers get an exception.
    public static Token? ReadNumber(byte[] data, ref int position, Recovery recovery)
    {
        var start = position;
        while (position < data.Length && !IsWhitespace(data[position]) && !IsDelimiter(data[position]))
        {
            position++;
        }

        var length = position - start;
        var hasDot = false;
        var hasDigit = false;
        for (var i = 0; i < length; i++)
        {
            var c = data[start + i];
            if (c == (byte)'+' || c == (byte)'-')
            {
                if (i != 0)
                {
                    return Malformed(start, recovery);
                }
            }
            else if (c == (byte)'.')
            {
                if (hasDot)
                {
                    return Malformed(start, recovery);
                }

                hasDot = true;
            }
            else if (c >= (byte)'0' && c <= (byte)'9')
            {
                hasDigit = true;
            }
            else
            {
                return Malformed(start, recovery);
            }
        }

        if (!hasDigit)
        {
            return Malformed(start, recovery);
        }

        var text = data.AsSpan(start, length);
        if (hasDot)
        {
            return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var real)
                ? Token.Real(real)
                : Malformed(start, recovery);
        }

        if (long.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var integer))
        {
            return Token.Integer(integer);
        }

        // An integer too large for long is out of range per ISO 32000-1 Annex C rather than
        // ungrammatical, so a lenient reader keeps the approximate magnitude.
        return recovery == Recovery.Lenient
            && double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var approximate)
            ? Token.Real(approximate)
            : Malformed(start, recovery);
    }

    private static Token? Malformed(int start, Recovery recovery)
        => recovery == Recovery.Lenient ? null : throw new DocumentParseException("Malformed number.", start);

    private Token ReadName() => Token.Name(ReadName(data, ref position));

    // ISO 32000-1 7.3.5: a name runs to the next whitespace or delimiter, with #XX decoding
    // to one byte. Names are the same in both grammars and cannot be malformed - a '#' that
    // is not followed by two hex digits is simply a literal '#' - so there is no recovery
    // parameter here.
    public static string ReadName(byte[] data, ref int position)
    {
        position++;
        var start = position;
        var escaped = false;
        while (position < data.Length && !IsWhitespace(data[position]) && !IsDelimiter(data[position]))
        {
            if (data[position] == (byte)'#' && position + 2 < data.Length
                && TryHex(data[position + 1], out _) && TryHex(data[position + 2], out _))
            {
                escaped = true;
                position += 3;
            }
            else
            {
                position++;
            }
        }

        return escaped ? DecodeEscapedName(data, start, position) : Decode(data, start, position - start);
    }

    private static string DecodeEscapedName(byte[] data, int start, int position)
    {
        var bytes = new List<byte>(position - start);
        var at = start;
        while (at < position)
        {
            var b = data[at];
            if (b == (byte)'#' && at + 2 < data.Length
                && TryHex(data[at + 1], out var hi) && TryHex(data[at + 2], out var lo))
            {
                bytes.Add((byte)((hi << 4) | lo));
                at += 3;
            }
            else
            {
                bytes.Add(b);
                at++;
            }
        }

        return Encoding.Latin1.GetString([.. bytes]);
    }

    private Token ReadLiteralString()
        => Token.String(TokenKind.StringLiteral, ReadLiteralString(data, ref position, Recovery.Strict));

    // ISO 32000-1 7.3.4.2: balanced parentheses, backslash escapes, octal escapes, and an
    // unescaped CR/LF/CRLF decoding to a single LF. Position enters on '(' and leaves past
    // the matching ')'; a lenient reader returns what it decoded when the string never closes.
    public static byte[] ReadLiteralString(byte[] data, ref int position, Recovery recovery)
    {
        var start = position;
        position++;
        var bytes = new List<byte>();
        var depth = 1;
        while (position < data.Length)
        {
            var b = data[position];
            position++;
            if (b == (byte)'\\')
            {
                if (position >= data.Length)
                {
                    break;
                }

                ReadStringEscape(data, ref position, bytes);
            }
            else if (b == (byte)'(')
            {
                depth++;
                bytes.Add(b);
            }
            else if (b == (byte)')')
            {
                depth--;
                if (depth == 0)
                {
                    return [.. bytes];
                }

                bytes.Add(b);
            }
            else if (b == 13)
            {
                bytes.Add(10);
                if (position < data.Length && data[position] == 10)
                {
                    position++;
                }
            }
            else
            {
                bytes.Add(b);
            }
        }

        return recovery == Recovery.Lenient
            ? [.. bytes]
            : throw new DocumentParseException("Unterminated string.", start);
    }

    private static void ReadStringEscape(byte[] data, ref int position, List<byte> bytes)
    {
        var e = data[position];
        position++;
        switch (e)
        {
            case (byte)'n':
                bytes.Add(10);
                break;
            case (byte)'r':
                bytes.Add(13);
                break;
            case (byte)'t':
                bytes.Add(9);
                break;
            case (byte)'b':
                bytes.Add(8);
                break;
            case (byte)'f':
                bytes.Add(12);
                break;
            case (byte)'(':
                bytes.Add((byte)'(');
                break;
            case (byte)')':
                bytes.Add((byte)')');
                break;
            case (byte)'\\':
                bytes.Add((byte)'\\');
                break;
            case 13:
                if (position < data.Length && data[position] == 10)
                {
                    position++;
                }

                break;
            case 10:
                break;
            default:
                if (e >= (byte)'0' && e <= (byte)'7')
                {
                    var value = e - '0';
                    for (var i = 0; i < 2 && position < data.Length
                        && data[position] >= (byte)'0' && data[position] <= (byte)'7'; i++)
                    {
                        value = (value << 3) | (data[position] - '0');
                        position++;
                    }

                    bytes.Add((byte)(value & 0xFF));
                }
                else
                {
                    bytes.Add(e);
                }

                break;
        }
    }

    private Token ReadHexString()
        => Token.String(TokenKind.HexString, ReadHexString(data, ref position, Recovery.Strict));

    // ISO 32000-1 7.3.4.3: position enters on '<' and leaves past the '>', which this grammar
    // requires. The digits themselves are read by ReadHexDigits.
    public static byte[] ReadHexString(byte[] data, ref int position, Recovery recovery)
    {
        var start = position;
        position++;
        return ReadHexDigits(data, ref position, recovery, recovery == Recovery.Lenient ? HexEnd.Optional : HexEnd.Required, start);
    }

    // ISO 32000-1 7.3.4.3 / 7.4.2: hex digit pairs, whitespace ignored, an odd trailing digit
    // padded with a zero, '>' ends. Position enters on the first digit and leaves past the '>'.
    public static byte[] ReadHexDigits(byte[] data, ref int position, Recovery recovery, HexEnd end, int start = -1)
    {
        if (start < 0)
        {
            start = position;
        }

        var bytes = new List<byte>();
        var hasHigh = false;
        var high = 0;
        while (position < data.Length)
        {
            var b = data[position];
            position++;
            if (b == (byte)'>')
            {
                return Flush(bytes, hasHigh, high);
            }

            if (IsWhitespace(b))
            {
                continue;
            }

            if (!TryHex(b, out var value))
            {
                if (recovery == Recovery.Lenient)
                {
                    continue;
                }

                throw new DocumentParseException("Malformed hexadecimal string.", position - 1);
            }

            if (hasHigh)
            {
                bytes.Add((byte)((high << 4) | value));
                hasHigh = false;
            }
            else
            {
                high = value;
                hasHigh = true;
            }
        }

        return end == HexEnd.Optional
            ? Flush(bytes, hasHigh, high)
            : throw new DocumentParseException("Unterminated hexadecimal string.", start);
    }

    private static byte[] Flush(List<byte> bytes, bool hasHigh, int high)
    {
        if (hasHigh)
        {
            bytes.Add((byte)(high << 4));
        }

        return [.. bytes];
    }

    private Token ReadKeyword()
    {
        var start = position;
        while (position < data.Length && !IsWhitespace(data[position]) && !IsDelimiter(data[position]))
        {
            position++;
        }

        if (position == start)
        {
            throw new DocumentParseException("Unexpected character.", start);
        }

        return Token.Keyword(Decode(data, start, position - start));
    }

    private static Dictionary<int, string[]> BuildInterned()
    {
        var buckets = new Dictionary<int, List<string>>();
        foreach (var token in InternedTokens)
        {
            var key = BucketKey(token.Length, (byte)token[0]);
            if (!buckets.TryGetValue(key, out var bucket))
            {
                buckets[key] = bucket = [];
            }

            bucket.Add(token);
        }

        var result = new Dictionary<int, string[]>(buckets.Count);
        foreach (var pair in buckets)
        {
            result[pair.Key] = [.. pair.Value];
        }

        return result;
    }

    private static int BucketKey(int length, byte first) => (length << 8) | first;

    // The same handful of names and keywords repeat once per object across the whole file,
    // so hand back the canonical instance instead of a fresh string for each occurrence.
    private static string Decode(byte[] data, int start, int length)
    {
        if (length is > 0 and <= MaxInternedLength
            && Interned.TryGetValue(BucketKey(length, data[start]), out var candidates))
        {
            foreach (var candidate in candidates)
            {
                var match = true;
                for (var i = 1; i < length; i++)
                {
                    if (candidate[i] != (char)data[start + i])
                    {
                        match = false;
                        break;
                    }
                }

                if (match)
                {
                    return candidate;
                }
            }
        }

        return Encoding.Latin1.GetString(data, start, length);
    }

    private static bool TryHex(byte b, out int value)
    {
        if (b >= (byte)'0' && b <= (byte)'9')
        {
            value = b - '0';
            return true;
        }

        if (b >= (byte)'A' && b <= (byte)'F')
        {
            value = b - 'A' + 10;
            return true;
        }

        if (b >= (byte)'a' && b <= (byte)'f')
        {
            value = b - 'a' + 10;
            return true;
        }

        value = 0;
        return false;
    }
}
