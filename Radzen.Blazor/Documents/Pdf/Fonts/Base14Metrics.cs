using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;

namespace Radzen.Documents.Pdf.Fonts;

// Metrics for the Adobe Core-14 (base-14) fonts, resolved from Base14Data.
internal sealed class Base14Metrics
{
    private static readonly Dictionary<string, Base14Data.Entry> EntryByName = BuildEntryIndex();
    private static readonly ConcurrentDictionary<string, Base14Metrics> Cache = new(StringComparer.Ordinal);

    private readonly Base14Data.Entry entry;
    private readonly Dictionary<string, int> widthByName;
    private readonly double[] widthByCode;

    private Base14Metrics(Base14Data.Entry entry)
    {
        this.entry = entry;
        widthByName = ParseWidths(entry.Widths);
        widthByCode = BuildCodeWidths(entry.FontName, widthByName);
    }

    public string PostScriptName => entry.FontName;

    public double CapHeight => entry.CapHeight;

    public double XHeight => entry.XHeight;

    public double Ascender => entry.Ascender;

    public double Descender => entry.Descender;

    public double ItalicAngle => entry.ItalicAngle;

    public bool IsFixedPitch => entry.IsFixedPitch;

    public double BBoxLeft => entry.BBoxLeft;

    public double BBoxBottom => entry.BBoxBottom;

    public double BBoxRight => entry.BBoxRight;

    public double BBoxTop => entry.BBoxTop;

    public static Base14Metrics? Resolve(Font font)
    {
        var psName = ResolvePostScriptName(font);
        if (psName == null || !EntryByName.TryGetValue(psName, out var entry))
        {
            return null;
        }

        return Cache.GetOrAdd(entry.FontName, _ => new Base14Metrics(entry));
    }

    public double GetWidth(byte code) => widthByCode[code];

    public double MeasureString(string text, double size)
    {
        double sum = 0;
        foreach (var c in text)
        {
            if (WinAnsiEncoding.TryGetCode(c, out var code))
            {
                sum += widthByCode[code];
            }
        }

        return sum * size / 1000.0;
    }

    public bool ContainsGlyph(char c) => WinAnsiEncoding.CanEncode(c);

    private static string? ResolvePostScriptName(Font font)
    {
        var name = font.Name;
        if (string.IsNullOrEmpty(name))
        {
            return null;
        }

        switch (name.ToLowerInvariant())
        {
            case "helvetica":
                return StyleSuffix(font, "Helvetica", "-Bold", "-Oblique", "-BoldOblique");
            case "courier":
                return StyleSuffix(font, "Courier", "-Bold", "-Oblique", "-BoldOblique");
            case "times":
            case "times-roman":
                return font.Bold && font.Italic ? "Times-BoldItalic"
                    : font.Bold ? "Times-Bold"
                    : font.Italic ? "Times-Italic"
                    : "Times-Roman";
            case "symbol":
                return "Symbol";
            case "zapfdingbats":
                return "ZapfDingbats";
            default:
                return EntryByName.ContainsKey(name) ? name : null;
        }
    }

    private static string StyleSuffix(Font font, string family, string bold, string italic, string boldItalic)
        => font.Bold && font.Italic ? family + boldItalic
            : font.Bold ? family + bold
            : font.Italic ? family + italic
            : family;

    private static Dictionary<string, int> ParseWidths(string widths)
    {
        var map = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var pair in widths.Split('|'))
        {
            var space = pair.LastIndexOf(' ');
            var name = pair[..space];
            var width = int.Parse(pair.AsSpan(space + 1), CultureInfo.InvariantCulture);
            map[name] = width;
        }

        return map;
    }

    private static double[] BuildCodeWidths(string fontName, Dictionary<string, int> widthByName)
    {
        var result = new double[256];
        var codeToName = NativeCodeMap(fontName);
        for (var code = 0; code < 256; code++)
        {
            string? glyph = codeToName != null
                ? (codeToName.TryGetValue((byte)code, out var n) ? n : null)
                : WinAnsiEncoding.GetGlyphName((byte)code);
            if (glyph != null && glyph != ".notdef" && widthByName.TryGetValue(glyph, out var w))
            {
                result[code] = w;
            }
        }

        return result;
    }

    private static IReadOnlyDictionary<byte, string>? NativeCodeMap(string fontName) => fontName switch
    {
        "Symbol" => SymbolEncodingData.Symbol,
        "ZapfDingbats" => SymbolEncodingData.ZapfDingbats,
        _ => null,
    };

    private static Dictionary<string, Base14Data.Entry> BuildEntryIndex()
    {
        var map = new Dictionary<string, Base14Data.Entry>(StringComparer.Ordinal);
        foreach (var entry in Base14Data.Fonts)
        {
            map[entry.FontName] = entry;
        }

        return map;
    }
}
