using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Radzen.Documents.Pdf;


// Shared content-stream tokenizer for the page-content grammar (operators, operands,
// arrays/dicts and whitespace-bounded BI/ID/EI inline-image skipping). Both
// ContentInterpreter and TextExtractor consume this stream; each consumer filters the
// tokens it cares about. This is the content-stream grammar, distinct from the PDF
// object/file grammar in Objects/Lexer.cs.
internal static class ContentTokenizer
{
    internal enum TokenKind
    {
        Number,
        Name,
        String,
        ArrayStart,
        ArrayEnd,
        DictStart,
        DictEnd,
        Operator,
    }

    internal readonly record struct Token(TokenKind Kind, double Number, string? Text, byte[]? Bytes);

    public static List<Token> Tokenize(byte[] data)
    {
        var tokens = new List<Token>();
        var position = 0;

        while (position < data.Length)
        {
            var b = data[position];

            if (IsWhitespace(b))
            {
                position++;
                continue;
            }

            switch (b)
            {
                case (byte)'%':
                    while (position < data.Length && data[position] != '\n' && data[position] != '\r')
                    {
                        position++;
                    }

                    continue;

                case (byte)'[':
                    tokens.Add(new Token(TokenKind.ArrayStart, 0, null, null));
                    position++;
                    continue;

                case (byte)']':
                    tokens.Add(new Token(TokenKind.ArrayEnd, 0, null, null));
                    position++;
                    continue;

                case (byte)'(':
                    tokens.Add(new Token(TokenKind.String, 0, null, ReadLiteralString(data, ref position)));
                    continue;

                case (byte)'/':
                    tokens.Add(new Token(TokenKind.Name, 0, ReadName(data, ref position), null));
                    continue;

                case (byte)'<':
                    if (position + 1 < data.Length && data[position + 1] == '<')
                    {
                        tokens.Add(new Token(TokenKind.DictStart, 0, null, null));
                        position += 2;
                        continue;
                    }

                    tokens.Add(new Token(TokenKind.String, 0, null, ReadHexString(data, ref position)));
                    continue;

                case (byte)'>':
                    if (position + 1 < data.Length && data[position + 1] == '>')
                    {
                        tokens.Add(new Token(TokenKind.DictEnd, 0, null, null));
                        position += 2;
                        continue;
                    }

                    position++;
                    continue;

                case (byte)'{':
                case (byte)'}':
                    position++;
                    continue;
            }

            if (IsNumberStart(b))
            {
                var start = position;
                while (position < data.Length && IsNumberChar(data[position]))
                {
                    position++;
                }

                var text = Latin1(data, start, position - start);
                if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
                {
                    tokens.Add(new Token(TokenKind.Number, value, null, null));
                    continue;
                }
            }

            var keywordStart = position;
            while (position < data.Length && !IsWhitespace(data[position]) && !IsDelimiter(data[position]))
            {
                position++;
            }

            if (position == keywordStart)
            {
                position++;
                continue;
            }

            var keyword = Latin1(data, keywordStart, position - keywordStart);
            if (keyword == "BI")
            {
                SkipInlineImage(data, ref position);
                continue;
            }

            tokens.Add(new Token(TokenKind.Operator, 0, keyword, null));
        }

        return tokens;
    }

    // After a BI operator, skip the inline-image dict and its binary payload, resuming
    // past the whitespace-delimited EI so the binary is not lexed as operators. EI may
    // occur inside the binary, so it only terminates when bounded by whitespace/EOF.
    private static void SkipInlineImage(byte[] data, ref int position)
    {
        while (position < data.Length)
        {
            if (data[position] == (byte)'I' && position + 1 < data.Length && data[position + 1] == (byte)'D'
                && (position == 0 || IsWhitespace(data[position - 1]) || IsDelimiter(data[position - 1]))
                && (position + 2 >= data.Length || IsWhitespace(data[position + 2]) || IsDelimiter(data[position + 2])))
            {
                position += 2;
                break;
            }

            position++;
        }

        if (position < data.Length && IsWhitespace(data[position]))
        {
            position++;
        }

        // EI ends the payload when bounded by whitespace/delimiter/EOF on the trailing side.
        // A preceding whitespace is the common case but not required: streams that pack the
        // image data flush against EI ("dataEI") must still terminate here rather than
        // swallowing the remainder of the content stream.
        while (position < data.Length)
        {
            if (data[position] == (byte)'E' && position + 1 < data.Length && data[position + 1] == (byte)'I'
                && (position + 2 >= data.Length || IsWhitespace(data[position + 2]) || IsDelimiter(data[position + 2])))
            {
                position += 2;
                return;
            }

            position++;
        }
    }

    private static byte[] ReadLiteralString(byte[] data, ref int position)
    {
        var bytes = new List<byte>();
        var depth = 0;
        position++;

        while (position < data.Length)
        {
            var b = data[position++];
            if (b == '\\')
            {
                if (position >= data.Length)
                {
                    break;
                }

                var e = data[position++];
                switch (e)
                {
                    case (byte)'n': bytes.Add((byte)'\n'); break;
                    case (byte)'r': bytes.Add((byte)'\r'); break;
                    case (byte)'t': bytes.Add((byte)'\t'); break;
                    case (byte)'b': bytes.Add((byte)'\b'); break;
                    case (byte)'f': bytes.Add((byte)'\f'); break;
                    case (byte)'(': bytes.Add((byte)'('); break;
                    case (byte)')': bytes.Add((byte)')'); break;
                    case (byte)'\\': bytes.Add((byte)'\\'); break;
                    case (byte)'\r':
                        if (position < data.Length && data[position] == '\n')
                        {
                            position++;
                        }

                        break;
                    case (byte)'\n':
                        break;
                    default:
                        if (e is >= (byte)'0' and <= (byte)'7')
                        {
                            var value = e - '0';
                            for (var k = 0; k < 2 && position < data.Length && data[position] is >= (byte)'0' and <= (byte)'7'; k++)
                            {
                                value = (value * 8) + (data[position++] - '0');
                            }

                            bytes.Add((byte)value);
                        }
                        else
                        {
                            bytes.Add(e);
                        }

                        break;
                }

                continue;
            }

            if (b == '(')
            {
                depth++;
                bytes.Add(b);
                continue;
            }

            if (b == ')')
            {
                if (depth == 0)
                {
                    break;
                }

                depth--;
                bytes.Add(b);
                continue;
            }

            bytes.Add(b);
        }

        return [.. bytes];
    }

    private static byte[] ReadHexString(byte[] data, ref int position)
    {
        var bytes = new List<byte>();
        position++;
        var high = -1;

        while (position < data.Length && data[position] != '>')
        {
            var b = data[position++];
            if (IsWhitespace(b))
            {
                continue;
            }

            var digit = HexDigit(b);
            if (digit < 0)
            {
                continue;
            }

            if (high < 0)
            {
                high = digit;
            }
            else
            {
                bytes.Add((byte)((high << 4) | digit));
                high = -1;
            }
        }

        if (high >= 0)
        {
            bytes.Add((byte)(high << 4));
        }

        if (position < data.Length)
        {
            position++;
        }

        return [.. bytes];
    }

    private static string ReadName(byte[] data, ref int position)
    {
        position++;
        var builder = new StringBuilder();
        while (position < data.Length && !IsWhitespace(data[position]) && !IsDelimiter(data[position]))
        {
            var b = data[position++];
            if (b == '#' && position + 1 < data.Length)
            {
                var hi = HexDigit(data[position]);
                var lo = HexDigit(data[position + 1]);
                if (hi >= 0 && lo >= 0)
                {
                    builder.Append((char)((hi << 4) | lo));
                    position += 2;
                    continue;
                }
            }

            builder.Append((char)b);
        }

        return builder.ToString();
    }

    private static int HexDigit(byte b) => b switch
    {
        >= (byte)'0' and <= (byte)'9' => b - '0',
        >= (byte)'a' and <= (byte)'f' => b - 'a' + 10,
        >= (byte)'A' and <= (byte)'F' => b - 'A' + 10,
        _ => -1,
    };

    private static string Latin1(byte[] data, int start, int length)
    {
        var chars = new char[length];
        for (var i = 0; i < length; i++)
        {
            chars[i] = (char)data[start + i];
        }

        return new string(chars);
    }

    private static bool IsWhitespace(byte b) => b is 0 or 9 or 10 or 12 or 13 or 32;

    private static bool IsDelimiter(byte b) => b is (byte)'(' or (byte)')' or (byte)'<' or (byte)'>'
        or (byte)'[' or (byte)']' or (byte)'{' or (byte)'}' or (byte)'/' or (byte)'%';

    private static bool IsNumberStart(byte b) => b is (byte)'+' or (byte)'-' or (byte)'.' or (>= (byte)'0' and <= (byte)'9');

    private static bool IsNumberChar(byte b) => b is (byte)'+' or (byte)'-' or (byte)'.' or (byte)'e' or (byte)'E' or (>= (byte)'0' and <= (byte)'9');
}
