using System.Collections.Generic;
using System.Text;
using Radzen.Documents.Pdf.Objects;

namespace Radzen.Documents.Pdf.Fonts;

// /ToUnicode CMap stream per ISO 32000-1 9.10.3: bfchar and bfrange sections.
internal static class ToUnicodeCMap
{
    public static (IReadOnlyDictionary<int, string> Map, int CodeBytes) Parse(byte[] data)
        => Parse(data, ReaderLimits.Default);

    public static (IReadOnlyDictionary<int, string> Map, int CodeBytes) Parse(byte[] data, ReaderLimits limits)
    {
        var map = new Dictionary<int, string>();
        var tokens = Tokenize(data);
        var codeBytes = 0;

        for (var i = 0; i < tokens.Count; i++)
        {
            switch (tokens[i].Keyword)
            {
                case "begincodespacerange":
                    for (i++; i < tokens.Count && tokens[i].Keyword != "endcodespacerange"; i++)
                    {
                        if (tokens[i].Hex is { } low)
                        {
                            if (codeBytes != 0 && codeBytes != low.Length)
                            {
                                throw new DocumentParseException(
                                    "ToUnicode CMap declares codespace ranges of differing byte widths, which are not supported.");
                            }

                            codeBytes = low.Length;
                            i++;
                        }
                    }

                    break;

                case "beginbfchar":
                    for (i++; i < tokens.Count && tokens[i].Keyword != "endbfchar"; i += 2)
                    {
                        if (tokens[i].Hex is { } src && i + 1 < tokens.Count && tokens[i + 1].Hex is { } dst)
                        {
                            Add(map, Code(src), Utf16(dst), limits);
                        }
                    }

                    break;

                case "beginbfrange":
                    i = ParseRange(tokens, i, map, limits);
                    break;
            }
        }

        return (map, codeBytes);
    }

    private static int ParseRange(List<Token> tokens, int index, Dictionary<int, string> map, ReaderLimits limits)
    {
        for (index++; index < tokens.Count && tokens[index].Keyword != "endbfrange";)
        {
            if (tokens[index].Hex is not { } lowBytes || index + 1 >= tokens.Count || tokens[index + 1].Hex is not { } highBytes)
            {
                index++;
                continue;
            }

            var low = Code(lowBytes);
            var high = Code(highBytes);
            index += 2;

            if (index < tokens.Count && tokens[index].IsArrayStart)
            {
                index++;
                for (var code = low; code <= high && index < tokens.Count && !tokens[index].IsArrayEnd; index++, code++)
                {
                    if (tokens[index].Hex is { } entry)
                    {
                        Add(map, code, Utf16(entry), limits);
                    }
                }

                while (index < tokens.Count && !tokens[index].IsArrayEnd)
                {
                    index++;
                }

                index++;
            }
            else if (index < tokens.Count && tokens[index].Hex is { } dst)
            {
                if (high >= low && (long)high - low + 1 > MaxCodespaceSpan(lowBytes.Length))
                {
                    throw new DocumentParseException("ToUnicode bfrange exceeds the permitted CMap size.");
                }

                CodeRangeExpander.Expand(low, high, map.Count, limits.MaxCMapEntries,
                    "ToUnicode bfrange exceeds the permitted CMap size.",
                    code => map[code] = Incremental(dst, code - low));

                index++;
            }
        }

        return index;
    }

    private static void Add(Dictionary<int, string> map, int code, string text, ReaderLimits limits)
    {
        if (map.Count >= limits.MaxCMapEntries && !map.ContainsKey(code))
        {
            throw new DocumentParseException("ToUnicode CMap exceeds the permitted CMap size.");
        }

        map[code] = text;
    }

    private static long MaxCodespaceSpan(int codeByteLength)
        => codeByteLength >= 4 ? 0x1_0000_0000L : 1L << (8 * (codeByteLength < 1 ? 1 : codeByteLength));

    private static int Code(byte[] bytes)
    {
        var value = 0;
        foreach (var b in bytes)
        {
            value = (value << 8) | b;
        }

        return value;
    }

    private static string Utf16(byte[] bytes)
    {
        var chars = new char[bytes.Length / 2];
        for (var i = 0; i + 1 < bytes.Length; i += 2)
        {
            chars[i / 2] = (char)((bytes[i] << 8) | bytes[i + 1]);
        }

        return new string(chars);
    }

    private static string Incremental(byte[] bytes, int offset)
    {
        var chars = Utf16(bytes).ToCharArray();
        if (chars.Length == 0)
        {
            return offset == 0 ? string.Empty : "\uFFFD";
        }

        var advanced = chars[^1] + offset;
        if (advanced is < 0 or > 0xFFFF)
        {
            return "\uFFFD";
        }

        chars[^1] = (char)advanced;
        var result = new string(chars);
        return IsWellFormedUtf16(result) ? result : "\uFFFD";
    }

    private static bool IsWellFormedUtf16(string value)
    {
        for (var i = 0; i < value.Length; i++)
        {
            if (char.IsHighSurrogate(value[i]))
            {
                if (i + 1 >= value.Length || !char.IsLowSurrogate(value[i + 1]))
                {
                    return false;
                }

                i++;
            }
            else if (char.IsLowSurrogate(value[i]))
            {
                return false;
            }
        }

        return true;
    }

    private static List<Token> Tokenize(byte[] data)
    {
        var tokens = new List<Token>();
        var position = 0;

        while (position < data.Length)
        {
            var b = data[position];

            if (Lexer.IsWhitespace(b))
            {
                position++;
                continue;
            }

            switch (b)
            {
                case (byte)'<':
                    tokens.Add(Token.FromHex(Lexer.ReadHexString(data, ref position, Lexer.Recovery.Lenient)));
                    continue;
                case (byte)'[':
                    tokens.Add(Token.ArrayStart);
                    position++;
                    continue;
                case (byte)']':
                    tokens.Add(Token.ArrayEnd);
                    position++;
                    continue;
                case (byte)'%':
                    while (position < data.Length && data[position] != '\n' && data[position] != '\r')
                    {
                        position++;
                    }

                    continue;
            }

            var start = position;
            while (position < data.Length && !IsBreak(data[position]))
            {
                position++;
            }

            if (position == start)
            {
                position++;
                continue;
            }

            tokens.Add(Token.FromKeyword(Encoding.Latin1.GetString(data, start, position - start)));
        }

        return tokens;
    }

    private static bool IsBreak(byte b)
        => Lexer.IsWhitespace(b) || b is (byte)'<' or (byte)'[' or (byte)']' or (byte)'%';

    private readonly record struct Token(string? Keyword, byte[]? Hex, bool IsArrayStart, bool IsArrayEnd)
    {
        public static Token ArrayStart { get; } = new(null, null, true, false);

        public static Token ArrayEnd { get; } = new(null, null, false, true);

        public static Token FromHex(byte[] hex) => new(null, hex, false, false);

        public static Token FromKeyword(string keyword) => new(keyword, null, false, false);
    }
}
