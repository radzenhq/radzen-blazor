#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Radzen.Documents.Pdf.Fonts;

namespace Radzen.Documents.Pdf;

// Resource-aware text extraction: re-walks a page content stream tracking the text
// and graphics matrices and the active font, reversing each shown char code to
// Unicode through the font's /ToUnicode, /Differences or WinAnsi encoding. Runs are
// emitted in reading order (descending Y, then ascending X).
internal static class TextExtractor
{
    private const double LineTolerance = 0.5;

    public static string Extract(byte[]? content, IReadOnlyDictionary<string, ReverseFont>? fonts)
    {
        if (content is null || content.Length == 0)
        {
            return string.Empty;
        }

        var fragments = new List<Fragment>();
        var tokens = Tokenize(content);

        var ctm = Matrix.Identity;
        var ctmStack = new Stack<Matrix>();
        var textMatrix = Matrix.Identity;
        var lineMatrix = Matrix.Identity;
        ReverseFont? font = null;

        var operands = new List<Token>();
        var buffer = new List<byte>();

        for (var i = 0; i < tokens.Count; i++)
        {
            var token = tokens[i];
            switch (token.Kind)
            {
                case TokenKind.Number:
                case TokenKind.Name:
                case TokenKind.Str:
                    operands.Add(token);
                    continue;

                case TokenKind.ArrayStart:
                    buffer.Clear();
                    for (i++; i < tokens.Count && tokens[i].Kind != TokenKind.ArrayEnd; i++)
                    {
                        if (tokens[i].Kind == TokenKind.Str)
                        {
                            buffer.AddRange(tokens[i].Bytes!);
                        }
                    }

                    operands.Add(new Token(TokenKind.Str, 0, null, [.. buffer]));
                    continue;

                case TokenKind.ArrayEnd:
                    continue;

                case TokenKind.Op:
                    break;
            }

            switch (token.Text)
            {
                case "q":
                    ctmStack.Push(ctm);
                    break;
                case "Q":
                    if (ctmStack.Count > 0)
                    {
                        ctm = ctmStack.Pop();
                    }

                    break;
                case "cm":
                    ctm = Components(operands) * ctm;
                    break;
                case "BT":
                    textMatrix = Matrix.Identity;
                    lineMatrix = Matrix.Identity;
                    break;
                case "Tf":
                    font = LastName(operands) is { } key && fonts is not null && fonts.TryGetValue(key, out var f)
                        ? f
                        : ReverseFont.WinAnsi;
                    break;
                case "Td":
                case "TD":
                    lineMatrix = Matrix.Translate(Number(operands, 0), Number(operands, 1)) * lineMatrix;
                    textMatrix = lineMatrix;
                    break;
                case "Tm":
                    lineMatrix = Components(operands);
                    textMatrix = lineMatrix;
                    break;
                case "T*":
                    textMatrix = lineMatrix;
                    break;
                case "Tj":
                case "TJ":
                case "'":
                case "\"":
                    Show(fragments, operands, textMatrix * ctm, font);
                    break;
            }

            operands.Clear();
        }

        return Compose(fragments);
    }

    private static void Show(List<Fragment> fragments, List<Token> operands, Matrix matrix, ReverseFont? font)
    {
        var bytes = LastString(operands);
        if (bytes is null || bytes.Length == 0)
        {
            return;
        }

        var text = (font ?? ReverseFont.WinAnsi).Decode(bytes);
        if (text.Length == 0)
        {
            return;
        }

        var origin = matrix.Transform(0, 0);
        fragments.Add(new Fragment(origin.Y, origin.X, text));
    }

    private static string Compose(List<Fragment> fragments)
    {
        if (fragments.Count == 0)
        {
            return string.Empty;
        }

        fragments.Sort(static (a, b) =>
        {
            if (Math.Abs(a.Y - b.Y) > LineTolerance)
            {
                return b.Y.CompareTo(a.Y);
            }

            return a.X.CompareTo(b.X);
        });

        var builder = new StringBuilder();
        double? lineY = null;
        foreach (var fragment in fragments)
        {
            if (lineY is { } y && Math.Abs(fragment.Y - y) > LineTolerance)
            {
                builder.Append('\n');
            }
            else if (lineY is not null)
            {
                builder.Append(' ');
            }

            builder.Append(fragment.Text);
            lineY = fragment.Y;
        }

        return builder.ToString();
    }

    private static Matrix Components(List<Token> operands)
    {
        var n = Numbers(operands, 6);
        return Matrix.FromComponents(n[0], n[1], n[2], n[3], n[4], n[5]);
    }

    private static double[] Numbers(List<Token> operands, int count)
    {
        var numbers = new List<double>(count);
        foreach (var token in operands)
        {
            if (token.Kind == TokenKind.Number)
            {
                numbers.Add(token.Number);
            }
        }

        var result = new double[count];
        var offset = numbers.Count - count;
        for (var i = 0; i < count; i++)
        {
            var index = offset + i;
            result[i] = index >= 0 && index < numbers.Count ? numbers[index] : 0.0;
        }

        return result;
    }

    private static double Number(List<Token> operands, int index)
    {
        var count = 0;
        foreach (var token in operands)
        {
            if (token.Kind == TokenKind.Number)
            {
                if (count == index)
                {
                    return token.Number;
                }

                count++;
            }
        }

        return 0.0;
    }

    private static string? LastName(List<Token> operands)
    {
        for (var i = operands.Count - 1; i >= 0; i--)
        {
            if (operands[i].Kind == TokenKind.Name)
            {
                return operands[i].Text;
            }
        }

        return null;
    }

    private static byte[]? LastString(List<Token> operands)
    {
        for (var i = operands.Count - 1; i >= 0; i--)
        {
            if (operands[i].Kind == TokenKind.Str)
            {
                return operands[i].Bytes;
            }
        }

        return null;
    }

    private static List<Token> Tokenize(byte[] data)
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
                    tokens.Add(new Token(TokenKind.Str, 0, null, ReadLiteralString(data, ref position)));
                    continue;
                case (byte)'/':
                    tokens.Add(new Token(TokenKind.Name, 0, ReadName(data, ref position), null));
                    continue;
                case (byte)'<':
                    if (position + 1 < data.Length && data[position + 1] == '<')
                    {
                        SkipDictionary(data, ref position);
                        continue;
                    }

                    tokens.Add(new Token(TokenKind.Str, 0, null, ReadHexString(data, ref position)));
                    continue;
                case (byte)'>':
                    position += position + 1 < data.Length && data[position + 1] == '>' ? 2 : 1;
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

            tokens.Add(new Token(TokenKind.Op, 0, keyword, null));
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

        while (position < data.Length)
        {
            if (IsWhitespace(data[position]) && position + 2 < data.Length
                && data[position + 1] == (byte)'E' && data[position + 2] == (byte)'I'
                && (position + 3 >= data.Length || IsWhitespace(data[position + 3]) || IsDelimiter(data[position + 3])))
            {
                position += 3;
                return;
            }

            position++;
        }
    }

    private static void SkipDictionary(byte[] data, ref int position)
    {
        position += 2;
        var depth = 1;
        while (position < data.Length && depth > 0)
        {
            if (data[position] == '<' && position + 1 < data.Length && data[position + 1] == '<')
            {
                depth++;
                position += 2;
                continue;
            }

            if (data[position] == '>' && position + 1 < data.Length && data[position + 1] == '>')
            {
                depth--;
                position += 2;
                continue;
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

    private enum TokenKind
    {
        Number,
        Name,
        Str,
        ArrayStart,
        ArrayEnd,
        Op,
    }

    private readonly record struct Token(TokenKind Kind, double Number, string? Text, byte[]? Bytes);

    private readonly record struct Fragment(double Y, double X, string Text);
}
