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

    private ReverseFont(int bytesPerCode, IReadOnlyDictionary<int, string> map)
    {
        this.bytesPerCode = bytesPerCode;
        this.map = map;
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
                decoded.Add(new DecodedCode(text, bytesPerCode == 1 && code == 32));
            }
        }

        return decoded;
    }

    public static ReverseFont Build(DocumentReader reader, DictionaryObject fontDict)
    {
        var subtype = Name(fontDict, "Subtype");
        if (string.Equals(subtype, "Type0", StringComparison.Ordinal))
        {
            var (unicode, codeBytes) = ToUnicode(reader, fontDict);
            return new ReverseFont(codeBytes > 0 ? codeBytes : 2, unicode);
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

        return new ReverseFont(1, simple);
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

    internal readonly record struct DecodedCode(string Text, bool IsWordSpace);
}
