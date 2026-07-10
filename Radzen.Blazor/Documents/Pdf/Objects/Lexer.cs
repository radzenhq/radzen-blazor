using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Radzen.Documents.Pdf.Objects;

internal sealed class Lexer
{
    private readonly byte[] data;
    private int position;

    public Lexer(byte[] data, int position)
    {
        this.data = data;
        this.position = position;
    }

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

        var raw = Encoding.Latin1.GetString(data, start, position - start);
        var hasDot = false;
        for (var i = 0; i < raw.Length; i++)
        {
            var c = raw[i];
            if (c == '+' || c == '-')
            {
                if (i != 0)
                {
                    throw new DocumentParseException("Malformed number.", start);
                }
            }
            else if (c == '.')
            {
                if (hasDot)
                {
                    throw new DocumentParseException("Malformed number.", start);
                }

                hasDot = true;
            }
            else if (c < '0' || c > '9')
            {
                throw new DocumentParseException("Malformed number.", start);
            }
        }

        if (hasDot)
        {
            var normalized = raw.EndsWith('.') ? raw + "0" : raw;
            if (!double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out var real))
            {
                throw new DocumentParseException("Malformed number.", start);
            }

            return Token.Real(real);
        }

        if (!long.TryParse(raw, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var integer))
        {
            throw new DocumentParseException("Malformed number.", start);
        }

        return Token.Integer(integer);
    }

    private Token ReadName()
    {
        position++;
        var bytes = new List<byte>();
        while (position < data.Length && !IsWhitespace(data[position]) && !IsDelimiter(data[position]))
        {
            var b = data[position];
            if (b == (byte)'#' && position + 2 < data.Length
                && TryHex(data[position + 1], out var hi) && TryHex(data[position + 2], out var lo))
            {
                bytes.Add((byte)((hi << 4) | lo));
                position += 3;
            }
            else
            {
                bytes.Add(b);
                position++;
            }
        }

        return Token.Name(Encoding.Latin1.GetString(bytes.ToArray()));
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
                    return Token.String(TokenKind.StringLiteral, bytes.ToArray());
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

                return Token.String(TokenKind.HexString, bytes.ToArray());
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

        return Token.Keyword(Encoding.Latin1.GetString(data, start, position - start));
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
