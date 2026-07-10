#nullable enable

using System.Collections.Generic;
using System.Globalization;

namespace Radzen.Blazor.Pdf.Tests;

// Minimal PDF content-stream tokenizer used only by the C3 tests to assert on the
// emitted operator stream. Not a product type.
internal enum ContentTokenKind
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

internal sealed class ContentToken
{
    public ContentTokenKind Kind { get; init; }

    public string Text { get; init; } = "";

    public byte[] Bytes { get; init; } = [];

    public double Number { get; init; }
}

internal sealed class ContentOperation
{
    public string Operator { get; init; } = "";

    public List<ContentToken> Operands { get; } = [];

    public double Num(int index) => Operands[index].Number;
}

internal static class ContentStreamTokenizer
{
    public static List<ContentToken> Tokenize(byte[] data)
    {
        var tokens = new List<ContentToken>();
        var i = 0;
        while (i < data.Length)
        {
            var b = data[i];
            if (IsWhitespace(b))
            {
                i++;
                continue;
            }

            if (b == (byte)'%')
            {
                while (i < data.Length && data[i] != (byte)'\n' && data[i] != (byte)'\r')
                {
                    i++;
                }

                continue;
            }

            switch (b)
            {
                case (byte)'(':
                    tokens.Add(ReadLiteralString(data, ref i));
                    break;
                case (byte)'<':
                    if (i + 1 < data.Length && data[i + 1] == (byte)'<')
                    {
                        i += 2;
                        tokens.Add(new ContentToken { Kind = ContentTokenKind.DictStart, Text = "<<" });
                    }
                    else
                    {
                        tokens.Add(ReadHexString(data, ref i));
                    }

                    break;
                case (byte)'>':
                    i += (i + 1 < data.Length && data[i + 1] == (byte)'>') ? 2 : 1;
                    tokens.Add(new ContentToken { Kind = ContentTokenKind.DictEnd, Text = ">>" });
                    break;
                case (byte)'[':
                    i++;
                    tokens.Add(new ContentToken { Kind = ContentTokenKind.ArrayStart, Text = "[" });
                    break;
                case (byte)']':
                    i++;
                    tokens.Add(new ContentToken { Kind = ContentTokenKind.ArrayEnd, Text = "]" });
                    break;
                case (byte)'/':
                    tokens.Add(ReadName(data, ref i));
                    break;
                default:
                    if (IsNumberStart(b))
                    {
                        tokens.Add(ReadNumberOrOperator(data, ref i));
                    }
                    else
                    {
                        tokens.Add(ReadOperator(data, ref i));
                    }

                    break;
            }
        }

        return tokens;
    }

    public static List<ContentOperation> Parse(byte[] data)
    {
        var operations = new List<ContentOperation>();
        var current = new ContentOperation();
        foreach (var token in Tokenize(data))
        {
            if (token.Kind == ContentTokenKind.Operator)
            {
                var op = new ContentOperation { Operator = token.Text };
                op.Operands.AddRange(current.Operands);
                operations.Add(op);
                current = new ContentOperation();
            }
            else
            {
                current.Operands.Add(token);
            }
        }

        return operations;
    }

    public static List<string> Operators(byte[] data)
    {
        var list = new List<string>();
        foreach (var op in Parse(data))
        {
            list.Add(op.Operator);
        }

        return list;
    }

    private static ContentToken ReadLiteralString(byte[] data, ref int i)
    {
        i++; // opening (
        var bytes = new List<byte>();
        var depth = 1;
        while (i < data.Length)
        {
            var b = data[i++];
            if (b == (byte)'\\')
            {
                if (i >= data.Length)
                {
                    break;
                }

                var e = data[i++];
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
                        if (i < data.Length && data[i] == (byte)'\n')
                        {
                            i++;
                        }

                        break;
                    case (byte)'\n':
                        break;
                    default:
                        if (e >= (byte)'0' && e <= (byte)'7')
                        {
                            var value = e - (byte)'0';
                            for (var k = 0; k < 2 && i < data.Length && data[i] >= (byte)'0' && data[i] <= (byte)'7'; k++)
                            {
                                value = (value * 8) + (data[i++] - (byte)'0');
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

            if (b == (byte)'(')
            {
                depth++;
                bytes.Add(b);
                continue;
            }

            if (b == (byte)')')
            {
                depth--;
                if (depth == 0)
                {
                    break;
                }

                bytes.Add(b);
                continue;
            }

            bytes.Add(b);
        }

        return new ContentToken { Kind = ContentTokenKind.String, Bytes = [.. bytes] };
    }

    private static ContentToken ReadHexString(byte[] data, ref int i)
    {
        i++; // opening <
        var bytes = new List<byte>();
        var high = -1;
        while (i < data.Length && data[i] != (byte)'>')
        {
            var b = data[i++];
            if (IsWhitespace(b))
            {
                continue;
            }

            var nibble = HexValue(b);
            if (nibble < 0)
            {
                continue;
            }

            if (high < 0)
            {
                high = nibble;
            }
            else
            {
                bytes.Add((byte)((high << 4) | nibble));
                high = -1;
            }
        }

        if (high >= 0)
        {
            bytes.Add((byte)(high << 4));
        }

        if (i < data.Length)
        {
            i++; // closing >
        }

        return new ContentToken { Kind = ContentTokenKind.String, Bytes = [.. bytes] };
    }

    private static ContentToken ReadName(byte[] data, ref int i)
    {
        i++; // slash
        var chars = new List<char>();
        while (i < data.Length && IsRegular(data[i]))
        {
            var b = data[i];
            if (b == (byte)'#' && i + 2 < data.Length)
            {
                var hi = HexValue(data[i + 1]);
                var lo = HexValue(data[i + 2]);
                if (hi >= 0 && lo >= 0)
                {
                    chars.Add((char)((hi << 4) | lo));
                    i += 3;
                    continue;
                }
            }

            chars.Add((char)b);
            i++;
        }

        return new ContentToken { Kind = ContentTokenKind.Name, Text = new string([.. chars]) };
    }

    private static ContentToken ReadNumberOrOperator(byte[] data, ref int i)
    {
        var start = i;
        while (i < data.Length && IsNumberChar(data[i]))
        {
            i++;
        }

        var text = System.Text.Encoding.ASCII.GetString(data, start, i - start);
        if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
        {
            return new ContentToken { Kind = ContentTokenKind.Number, Text = text, Number = value };
        }

        return new ContentToken { Kind = ContentTokenKind.Operator, Text = text };
    }

    private static ContentToken ReadOperator(byte[] data, ref int i)
    {
        var start = i;
        while (i < data.Length && IsRegular(data[i]))
        {
            i++;
        }

        if (i == start)
        {
            i++; // never stall on an unexpected delimiter
        }

        return new ContentToken
        {
            Kind = ContentTokenKind.Operator,
            Text = System.Text.Encoding.ASCII.GetString(data, start, i - start),
        };
    }

    private static bool IsWhitespace(byte b) => b is 0 or 9 or 10 or 12 or 13 or 32;

    private static bool IsDelimiter(byte b) =>
        b is (byte)'(' or (byte)')' or (byte)'<' or (byte)'>' or (byte)'[' or (byte)']'
        or (byte)'{' or (byte)'}' or (byte)'/' or (byte)'%';

    private static bool IsRegular(byte b) => !IsWhitespace(b) && !IsDelimiter(b);

    private static bool IsNumberStart(byte b) =>
        (b >= (byte)'0' && b <= (byte)'9') || b == (byte)'+' || b == (byte)'-' || b == (byte)'.';

    private static bool IsNumberChar(byte b) =>
        (b >= (byte)'0' && b <= (byte)'9') || b == (byte)'+' || b == (byte)'-'
        || b == (byte)'.' || b == (byte)'e' || b == (byte)'E';

    private static int HexValue(byte b) => b switch
    {
        >= (byte)'0' and <= (byte)'9' => b - (byte)'0',
        >= (byte)'a' and <= (byte)'f' => b - (byte)'a' + 10,
        >= (byte)'A' and <= (byte)'F' => b - (byte)'A' + 10,
        _ => -1,
    };
}
