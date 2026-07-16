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
                if (b == (byte)'+' || b == (byte)'-' || b == (byte)'.' || (b >= (byte)'0' && b <= (byte)'9'))
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

    private Token ReadNumber()
    {
        var start = position;
        while (position < data.Length && !IsWhitespace(data[position]) && !IsDelimiter(data[position]))
        {
            position++;
        }

        var length = position - start;
        var hasDot = false;
        for (var i = 0; i < length; i++)
        {
            var c = data[start + i];
            if (c == (byte)'+' || c == (byte)'-')
            {
                if (i != 0)
                {
                    throw new DocumentParseException("Malformed number.", start);
                }
            }
            else if (c == (byte)'.')
            {
                if (hasDot)
                {
                    throw new DocumentParseException("Malformed number.", start);
                }

                hasDot = true;
            }
            else if (c < (byte)'0' || c > (byte)'9')
            {
                throw new DocumentParseException("Malformed number.", start);
            }
        }

        // A trailing '.' is legal in PDF but not to TryParse, so leave room to append '0'.
        var chars = length < 128 ? stackalloc char[length + 1] : new char[length + 1];
        for (var i = 0; i < length; i++)
        {
            chars[i] = (char)data[start + i];
        }

        if (hasDot)
        {
            var text = chars[..length];
            if (data[position - 1] == (byte)'.')
            {
                chars[length] = '0';
                text = chars;
            }

            if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var real))
            {
                throw new DocumentParseException("Malformed number.", start);
            }

            return Token.Real(real);
        }

        if (!long.TryParse(chars[..length], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var integer))
        {
            throw new DocumentParseException("Malformed number.", start);
        }

        return Token.Integer(integer);
    }

    private Token ReadName()
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

        return Token.Name(escaped ? DecodeEscapedName(start) : Decode(start, position - start));
    }

    private string DecodeEscapedName(int start)
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

                ReadStringEscape(bytes);
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
                    return Token.String(TokenKind.StringLiteral, [.. bytes]);
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

        throw new DocumentParseException("Unterminated string.", start);
    }

    private void ReadStringEscape(List<byte> bytes)
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
    {
        var start = position;
        position++;
        var bytes = new List<byte>();
        var hasHigh = false;
        var high = 0;
        while (position < data.Length)
        {
            var b = data[position];
            position++;
            if (b == (byte)'>')
            {
                if (hasHigh)
                {
                    bytes.Add((byte)(high << 4));
                }

                return Token.String(TokenKind.HexString, [.. bytes]);
            }

            if (IsWhitespace(b))
            {
                continue;
            }

            if (!TryHex(b, out var value))
            {
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

        throw new DocumentParseException("Unterminated hexadecimal string.", start);
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

        return Token.Keyword(Decode(start, position - start));
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
    private string Decode(int start, int length)
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
