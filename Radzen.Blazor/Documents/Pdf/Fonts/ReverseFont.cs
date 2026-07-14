using System;
using System.Collections.Generic;
using System.Text;
using Radzen.Documents.Pdf.Objects;

namespace Radzen.Documents.Pdf.Fonts;

// A char-code -> Unicode reverse mapping for a single font resource, used by text
// extraction. Simple fonts consume one byte per code and map through their WinAnsi
// base encoding overlaid with any /Differences; Type0/Identity-H fonts consume two
// bytes per code and map through the emitted /ToUnicode CMap.
internal sealed class ReverseFont
{
    private readonly int bytesPerCode;
    private readonly IReadOnlyDictionary<int, string> map;
    private readonly IReadOnlyDictionary<int, double>? widths;
    private readonly double? defaultWidth;

    private ReverseFont(int bytesPerCode, IReadOnlyDictionary<int, string> map,
        IReadOnlyDictionary<int, double>? widths = null, double? defaultWidth = null)
    {
        this.bytesPerCode = bytesPerCode;
        this.map = map;
        this.widths = widths;
        this.defaultWidth = defaultWidth;
    }

    public static ReverseFont WinAnsi { get; } = new(1, BuildWinAnsiMap());

    public static ReverseFont FromGlyphIds(IReadOnlyDictionary<ushort, int> gidToUnicode)
    {
        var map = new Dictionary<int, string>(gidToUnicode.Count);
        foreach (var entry in gidToUnicode)
        {
            map[entry.Key] = entry.Value is >= 0 and <= 0x10FFFF and (< 0xD800 or > 0xDFFF)
                ? char.ConvertFromUtf32(entry.Value)
                : "\uFFFD";
        }

        return new ReverseFont(2, map);
    }

    public static ReverseFont FromBase14(string name)
        => new(1, BuildWinAnsiMap(), BuildBase14Widths(name));

    public string Decode(byte[] codes)
    {
        var builder = new StringBuilder(codes.Length);
        for (var i = 0; i + bytesPerCode <= codes.Length; i += bytesPerCode)
        {
            var code = 0;
            for (var j = 0; j < bytesPerCode; j++)
            {
                code = (code << 8) | codes[i + j];
            }

            if (map.TryGetValue(code, out var text))
            {
                builder.Append(text);
            }
        }

        return builder.ToString();
    }

    internal IReadOnlyList<DecodedCode> DecodeCodes(byte[] codes)
    {
        if (codes.Length % bytesPerCode != 0)
        {
            throw new FormatException("The text string ends with an incomplete character code.");
        }

        var decoded = new List<DecodedCode>(codes.Length / bytesPerCode);
        for (var i = 0; i < codes.Length; i += bytesPerCode)
        {
            var code = 0;
            for (var j = 0; j < bytesPerCode; j++)
            {
                code = (code << 8) | codes[i + j];
            }

            if (map.TryGetValue(code, out var text))
            {
                decoded.Add(new DecodedCode(code, text, bytesPerCode == 1 && code == 32));
            }
        }

        return decoded;
    }

    internal bool TryEncode(string text, out byte[] codes)
    {
        var reverse = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var entry in map)
        {
            reverse.TryAdd(entry.Value, entry.Key);
        }

        var result = new List<byte>();
        for (var offset = 0; offset < text.Length;)
        {
            var foundCode = -1;
            var foundLength = 0;
            foreach (var entry in reverse)
            {
                if (entry.Key.Length > foundLength && text.AsSpan(offset).StartsWith(entry.Key, StringComparison.Ordinal))
                {
                    foundCode = entry.Value;
                    foundLength = entry.Key.Length;
                }
            }

            if (foundCode < 0)
            {
                codes = [];
                return false;
            }

            for (var shift = (bytesPerCode - 1) * 8; shift >= 0; shift -= 8)
            {
                result.Add((byte)(foundCode >> shift));
            }

            offset += foundLength;
        }

        codes = [.. result];
        return true;
    }

    internal bool TryGetWidth(int code, out double width)
    {
        if (widths is not null && widths.TryGetValue(code, out width) && double.IsFinite(width) && width >= 0)
        {
            return true;
        }

        if (defaultWidth is { } fallback && double.IsFinite(fallback) && fallback >= 0)
        {
            width = fallback;
            return true;
        }

        width = 0;
        return false;
    }

    public static ReverseFont Build(DocumentReader reader, DictionaryObject fontDict)
    {
        var subtype = Name(fontDict, "Subtype");
        if (string.Equals(subtype, "Type0", StringComparison.Ordinal))
        {
            var (unicode, codeBytes) = ToUnicode(reader, fontDict);
            var (widths, defaultWidth) = CidWidths(reader, fontDict);
            return new ReverseFont(codeBytes > 0 ? codeBytes : 2, unicode, widths, defaultWidth);
        }

        var simple = new Dictionary<int, string>(256);
        for (var code = 0; code < 256; code++)
        {
            if (WinAnsiEncoding.TryGetChar((byte)code, out var c))
            {
                simple[code] = c.ToString();
            }
        }

        ApplyEncoding(reader, fontDict, simple);

        var (toUnicode, _) = ToUnicode(reader, fontDict);
        foreach (var entry in toUnicode)
        {
            simple[entry.Key] = entry.Value;
        }

        var simpleWidths = SimpleWidths(reader, fontDict);
        return new ReverseFont(1, simple, simpleWidths);
    }

    private static IReadOnlyDictionary<int, double>? SimpleWidths(DocumentReader reader, DictionaryObject fontDict)
    {
        if (reader.GetArray(fontDict, "Widths") is { } array)
        {
            var first = reader.GetInt(fontDict, "FirstChar") ?? 0;
            var result = new Dictionary<int, double>(array.Count);
            for (var i = 0; i < array.Count; i++)
            {
                if (reader.Resolve(array[i]) is NumberObject number && number.DoubleValue >= 0)
                {
                    result[first + i] = number.DoubleValue;
                }
            }

            return result;
        }

        return Name(fontDict, "BaseFont") is { } baseFont ? BuildBase14Widths(baseFont) : null;
    }

    private static (IReadOnlyDictionary<int, double>? Widths, double? DefaultWidth) CidWidths(DocumentReader reader, DictionaryObject fontDict)
    {
        if (reader.GetArray(fontDict, "DescendantFonts") is not { Count: > 0 } descendants
            || reader.AsDictionary(descendants[0]) is not { } descendant)
        {
            return (null, null);
        }

        var result = new Dictionary<int, double>();
        if (reader.GetArray(descendant, "W") is { } widths)
        {
            for (var i = 0; i < widths.Count;)
            {
                if (reader.Resolve(widths[i++]) is not NumberObject startObject)
                {
                    throw new FormatException("A CID font /W array has an invalid starting CID.");
                }

                var start = startObject.IntValue;
                var next = i < widths.Count ? reader.Resolve(widths[i++]) : null;
                if (next is ArrayObject run)
                {
                    for (var offset = 0; offset < run.Count; offset++)
                    {
                        if (reader.Resolve(run[offset]) is not NumberObject width)
                        {
                            throw new FormatException("A CID font /W array contains a non-numeric width.");
                        }

                        result[start + offset] = width.DoubleValue;
                    }
                }
                else if (next is NumberObject endObject && i < widths.Count
                    && reader.Resolve(widths[i++]) is NumberObject width)
                {
                    for (var cid = start; cid <= endObject.IntValue; cid++)
                    {
                        result[cid] = width.DoubleValue;
                    }
                }
                else
                {
                    throw new FormatException("A CID font /W array is malformed.");
                }
            }
        }

        return (result, reader.GetNumber(descendant, "DW") ?? 1000.0);
    }

    private static IReadOnlyDictionary<int, double>? BuildBase14Widths(string name)
    {
        var metrics = Base14Metrics.Resolve(new Font { Name = name });
        if (metrics is null)
        {
            return null;
        }

        var result = new Dictionary<int, double>(256);
        for (var code = 0; code < 256; code++)
        {
            result[code] = metrics.GetWidth((byte)code);
        }

        return result;
    }

    private static void ApplyEncoding(DocumentReader reader, DictionaryObject fontDict, Dictionary<int, string> map)
    {
        if (reader.GetDictionary(fontDict, "Encoding") is not { } encoding
            || reader.GetArray(encoding, "Differences") is not { } differences)
        {
            return;
        }

        var code = 0;
        foreach (var item in differences)
        {
            var resolved = reader.Resolve(item);
            if (resolved is NumberObject number)
            {
                code = number.IntValue;
            }
            else if (resolved is NameObject name)
            {
                if (WinAnsiEncoding.TryGetCodePointByName(name.Value, out var cp))
                {
                    map[code] = char.ConvertFromUtf32(cp);
                }
                else
                {
                    map.Remove(code);
                }

                code++;
            }
        }
    }

    private static (IReadOnlyDictionary<int, string> Map, int CodeBytes) ToUnicode(DocumentReader reader, DictionaryObject fontDict)
    {
        if (reader.GetStream(fontDict, "ToUnicode") is not { } stream)
        {
            return (EmptyMap, 0);
        }

        return ToUnicodeCMap.Parse(reader.DecodeStream(stream), reader.Limits);
    }

    private static Dictionary<int, string> BuildWinAnsiMap()
    {
        var map = new Dictionary<int, string>(256);
        for (var code = 0; code < 256; code++)
        {
            if (WinAnsiEncoding.TryGetChar((byte)code, out var c))
            {
                map[code] = c.ToString();
            }
        }

        return map;
    }

    private static readonly Dictionary<int, string> EmptyMap = [];

    private static string? Name(DictionaryObject dictionary, string key)
        => dictionary.TryGetValue(key, out var value) && value is NameObject name ? name.Value : null;

    internal readonly record struct DecodedCode(int Code, string Text, bool IsWordSpace);
}
