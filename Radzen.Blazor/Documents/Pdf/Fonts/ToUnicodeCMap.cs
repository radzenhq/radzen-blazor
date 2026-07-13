using System.Collections.Generic;
using Radzen.Documents.Pdf.Objects;

namespace Radzen.Documents.Pdf.Fonts;

// Parses a /ToUnicode CMap stream (ISO 32000-1 9.10.3): the bfchar and bfrange
// sections that map source char codes to UTF-16BE Unicode sequences. Returns the
// code -> string map plus the source code byte width taken from the code space range.
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
                            // ReverseFont decodes every code at one fixed width. A CMap that
                            // mixes widths (e.g. a 1-byte and a 2-byte range, common in CJK)
                            // cannot be decoded correctly at a single width, so fail loud
                            // rather than pairwise-merge or split codes into garbage text.
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
                            map[Code(src)] = Utf16(dst);
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
                        map[code] = Utf16(entry);
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
                if (high < low)
                {
                    index++;
                    continue;
                }

                // A well-formed incremental bfrange walks one contiguous run inside the
                // source codespace; an attacker-sized span (e.g. <0000> <7fffffff>) would
                // otherwise materialize billions of dictionary entries and exhaust memory.
                var span = (long)high - low + 1;
                if (span > MaxCodespaceSpan(lowBytes.Length)
                    || (long)map.Count + span > limits.MaxCMapEntries)
                {
                    throw new DocumentParseException("ToUnicode bfrange exceeds the permitted CMap size.");
                }

                for (var code = low; code <= high; code++)
                {
                    map[code] = Incremental(dst, code - low);
                }

                index++;
            }
        }

        return index;
    }

    // Number of code points addressable by a source code of the given byte width;
    // a 2-byte code covers at most 0x10000 entries. Codes wider than 4 bytes are
    // clamped to the incremental cap below, which rejects the span outright.
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

    // Incremental bfrange destination: decode as UTF-16BE (the bfchar form) and
    // advance the LAST code unit by offset, so a surrogate-pair base like <D835DC00>
    // walks the low surrogate and stays a valid supra-BMP scalar. A malformed or
    // lone-surrogate result falls back to U+FFFD rather than throwing.
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

            if (b is 0 or 9 or 10 or 12 or 13 or 32)
            {
                position++;
                continue;
            }

            switch (b)
            {
                case (byte)'<':
                    tokens.Add(Token.FromHex(ReadHex(data, ref position)));
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

            tokens.Add(Token.FromKeyword(Latin1(data, start, position - start)));
        }

        return tokens;
    }

    private static byte[] ReadHex(byte[] data, ref int position)
    {
        position++;
        var bytes = new List<byte>();
        var high = -1;

        while (position < data.Length && data[position] != '>')
        {
            var digit = HexDigit(data[position++]);
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

    private static int HexDigit(byte b) => b switch
    {
        >= (byte)'0' and <= (byte)'9' => b - '0',
        >= (byte)'a' and <= (byte)'f' => b - 'a' + 10,
        >= (byte)'A' and <= (byte)'F' => b - 'A' + 10,
        _ => -1,
    };

    private static bool IsBreak(byte b) => b is 0 or 9 or 10 or 12 or 13 or 32 or (byte)'<' or (byte)'[' or (byte)']' or (byte)'%';

    private static string Latin1(byte[] data, int start, int length)
    {
        var chars = new char[length];
        for (var i = 0; i < length; i++)
        {
            chars[i] = (char)data[start + i];
        }

        return new string(chars);
    }

    private readonly record struct Token(string? Keyword, byte[]? Hex, bool IsArrayStart, bool IsArrayEnd)
    {
        public static Token ArrayStart { get; } = new(null, null, true, false);

        public static Token ArrayEnd { get; } = new(null, null, false, true);

        public static Token FromHex(byte[] hex) => new(null, hex, false, false);

        public static Token FromKeyword(string keyword) => new(keyword, null, false, false);
    }
}
