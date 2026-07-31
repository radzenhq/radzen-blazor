using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System;

namespace Radzen.Documents.Fonts;

internal sealed class BuiltInFontMetrics
{
    private static readonly Dictionary<string, BuiltInFontData.Entry> EntryByName = BuildEntryIndex();
    private static readonly Dictionary<string, string> KernByName = BuildKernIndex();
    private static readonly ConcurrentDictionary<string, BuiltInFontMetrics> Cache = new(StringComparer.Ordinal);

    private readonly BuiltInFontData.Entry entry;
    private readonly Dictionary<string, int> widthByName;
    private readonly Dictionary<int, int> kernByPair;

    private BuiltInFontMetrics(BuiltInFontData.Entry entry)
    {
        this.entry = entry;
        widthByName = ParseWidths(entry.Widths);
        kernByPair = ParseKerning(entry.FontName);
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

    public static BuiltInFontMetrics? Resolve(Font font)
    {
        var psName = ResolvePostScriptName(
            font.EffectiveFamily,
            font.EffectiveBold,
            font.EffectiveItalic);
        return Resolve(psName);
    }

    public static BuiltInFontMetrics? Resolve(in FontPaint font)
        => Resolve(ResolvePostScriptName(font.Family, font.Bold, font.Italic));

    private static BuiltInFontMetrics? Resolve(string? psName)
    {
        if (psName == null || !EntryByName.TryGetValue(psName, out var entry))
        {
            return null;
        }

        return Cache.GetOrAdd(entry.FontName, _ => new BuiltInFontMetrics(entry));
    }

    public double GetWidth(string glyphName)
        => widthByName.TryGetValue(glyphName, out var width) ? width : 0;

    public bool TryGetWidth(int codepoint, out double width)
    {
        var glyphName = GlyphName(codepoint);
        if (glyphName is not null && widthByName.TryGetValue(glyphName, out var value))
        {
            width = value;
            return true;
        }

        width = 0;
        return false;
    }

    public double GetKerning(char left, char right)
        => kernByPair.TryGetValue(FontMetric.PairKey(left, right), out var value) ? value : 0.0;

    public double GetRunKerning(char left, char right)
        => left == ' ' || right == ' ' ? 0.0 : GetKerning(left, right);

    public double MeasureString(string text, double size)
    {
        double sum = 0;
        var index = 0;
        List<int>? missing = null;
        while (index < text.Length)
        {
            var codepoint = FontCollection.CodePointAt(text, index, out var length);
            if (TryGetWidth(codepoint, out var width)
                || (!MissingGlyphMetrics.IsReportable(codepoint) && TryGetWidth('?', out width)))
            {
                sum += width;
            }
            else if (missing is null || !missing.Contains(codepoint))
            {
                (missing ??= []).Add(codepoint);
            }

            index += length;
        }

        if (missing is not null)
        {
            throw MissingGlyphMetrics.Error(entry.FontName, missing);
        }

        return FontMetric.Scale(sum, size, 1000);
    }

    private static string? ResolvePostScriptName(string name, bool bold, bool italic)
    {
        if (string.IsNullOrEmpty(name))
        {
            return null;
        }

        return name.ToLowerInvariant() switch
        {
            "helvetica" => StyleSuffix(bold, italic, "Helvetica", "-Bold", "-Oblique", "-BoldOblique"),
            "courier" => StyleSuffix(bold, italic, "Courier", "-Bold", "-Oblique", "-BoldOblique"),
            "times" or "times-roman" => bold && italic ? "Times-BoldItalic"
                                : bold ? "Times-Bold"
                                : italic ? "Times-Italic"
                                : "Times-Roman",
            "symbol" => "Symbol",
            "zapfdingbats" => "ZapfDingbats",
            _ => EntryByName.ContainsKey(name) ? name : null,
        };
    }

    private static string StyleSuffix(
        bool isBold,
        bool isItalic,
        string family,
        string bold,
        string italic,
        string boldItalic)
        => isBold && isItalic ? family + boldItalic
            : isBold ? family + bold
            : isItalic ? family + italic
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

    private string? GlyphName(int codepoint)
    {
        if (entry.FontName == "Symbol"
            && codepoint <= byte.MaxValue
            && BuiltInFontGlyphData.Symbol.TryGetValue((byte)codepoint, out var symbol))
        {
            return symbol;
        }

        if (entry.FontName == "ZapfDingbats"
            && codepoint <= byte.MaxValue
            && BuiltInFontGlyphData.ZapfDingbats.TryGetValue((byte)codepoint, out var dingbat))
        {
            return dingbat;
        }

        return UnicodeGlyphName(codepoint);
    }

    private static string? UnicodeGlyphName(int codepoint)
    {
        if (SpecialGlyphNames.TryGetValue(codepoint, out var special))
        {
            return special;
        }

        if ((codepoint >= 'A' && codepoint <= 'Z')
            || (codepoint >= 'a' && codepoint <= 'z'))
        {
            return ((char)codepoint).ToString();
        }

        if (codepoint > char.MaxValue)
        {
            return null;
        }

        var decomposed = ((char)codepoint).ToString().Normalize(NormalizationForm.FormD);
        if (decomposed.Length == 2
            && CombiningSuffixes.TryGetValue(decomposed[1], out var suffix))
        {
            return decomposed[0] + suffix;
        }

        return null;
    }

    private static readonly IReadOnlyDictionary<char, string> CombiningSuffixes = new Dictionary<char, string>
    {
        ['\u0300'] = "grave",
        ['\u0301'] = "acute",
        ['\u0302'] = "circumflex",
        ['\u0303'] = "tilde",
        ['\u0304'] = "macron",
        ['\u0306'] = "breve",
        ['\u0307'] = "dotaccent",
        ['\u0308'] = "dieresis",
        ['\u030A'] = "ring",
        ['\u030B'] = "hungarumlaut",
        ['\u030C'] = "caron",
        ['\u0326'] = "commaaccent",
        ['\u0327'] = "cedilla",
        ['\u0328'] = "ogonek",
    };

    private static readonly IReadOnlyDictionary<int, string> SpecialGlyphNames = new Dictionary<int, string>
    {
        [' '] = "space",
        ['!'] = "exclam",
        ['"'] = "quotedbl",
        ['#'] = "numbersign",
        ['$'] = "dollar",
        ['%'] = "percent",
        ['&'] = "ampersand",
        ['\''] = "quotesingle",
        ['('] = "parenleft",
        [')'] = "parenright",
        ['*'] = "asterisk",
        ['+'] = "plus",
        [','] = "comma",
        ['-'] = "hyphen",
        ['.'] = "period",
        ['/'] = "slash",
        ['0'] = "zero",
        ['1'] = "one",
        ['2'] = "two",
        ['3'] = "three",
        ['4'] = "four",
        ['5'] = "five",
        ['6'] = "six",
        ['7'] = "seven",
        ['8'] = "eight",
        ['9'] = "nine",
        [':'] = "colon",
        [';'] = "semicolon",
        ['<'] = "less",
        ['='] = "equal",
        ['>'] = "greater",
        ['?'] = "question",
        ['@'] = "at",
        ['['] = "bracketleft",
        ['\\'] = "backslash",
        [']'] = "bracketright",
        ['^'] = "asciicircum",
        ['_'] = "underscore",
        ['`'] = "grave",
        ['{'] = "braceleft",
        ['|'] = "bar",
        ['}'] = "braceright",
        ['~'] = "asciitilde",
        [0x00A0] = "space",
        [0x00A1] = "exclamdown",
        [0x00A2] = "cent",
        [0x00A3] = "sterling",
        [0x00A4] = "currency",
        [0x00A5] = "yen",
        [0x00A6] = "brokenbar",
        [0x00A7] = "section",
        [0x00A8] = "dieresis",
        [0x00A9] = "copyright",
        [0x00AA] = "ordfeminine",
        [0x00AB] = "guillemotleft",
        [0x00AC] = "logicalnot",
        [0x00AD] = "hyphen",
        [0x00AE] = "registered",
        [0x00AF] = "macron",
        [0x00B0] = "degree",
        [0x00B1] = "plusminus",
        [0x00B2] = "twosuperior",
        [0x00B3] = "threesuperior",
        [0x00B4] = "acute",
        [0x00B5] = "mu",
        [0x00B6] = "paragraph",
        [0x00B7] = "periodcentered",
        [0x00B8] = "cedilla",
        [0x00B9] = "onesuperior",
        [0x00BA] = "ordmasculine",
        [0x00BB] = "guillemotright",
        [0x00BC] = "onequarter",
        [0x00BD] = "onehalf",
        [0x00BE] = "threequarters",
        [0x00BF] = "questiondown",
        [0x00C6] = "AE",
        [0x00D0] = "Eth",
        [0x00D7] = "multiply",
        [0x00D8] = "Oslash",
        [0x00DE] = "Thorn",
        [0x00DF] = "germandbls",
        [0x00E6] = "ae",
        [0x00F0] = "eth",
        [0x00F7] = "divide",
        [0x00F8] = "oslash",
        [0x00FE] = "thorn",
        [0x0110] = "Dcroat",
        [0x0111] = "dcroat",
        [0x0131] = "dotlessi",
        [0x0141] = "Lslash",
        [0x0142] = "lslash",
        [0x0152] = "OE",
        [0x0153] = "oe",
        [0x0192] = "florin",
        [0x02C6] = "circumflex",
        [0x02DC] = "tilde",
        [0x2013] = "endash",
        [0x2014] = "emdash",
        [0x2018] = "quoteleft",
        [0x2019] = "quoteright",
        [0x201A] = "quotesinglbase",
        [0x201C] = "quotedblleft",
        [0x201D] = "quotedblright",
        [0x201E] = "quotedblbase",
        [0x2020] = "dagger",
        [0x2021] = "daggerdbl",
        [0x2022] = "bullet",
        [0x2026] = "ellipsis",
        [0x2030] = "perthousand",
        [0x2039] = "guilsinglleft",
        [0x203A] = "guilsinglright",
        [0x20AC] = "Euro",
        [0x2122] = "trademark",
        [0x2202] = "partialdiff",
        [0x2206] = "Delta",
        [0x2211] = "summation",
        [0x2212] = "minus",
        [0x221A] = "radical",
        [0x221E] = "infinity",
        [0x2260] = "notequal",
        [0x2264] = "lessequal",
        [0x2265] = "greaterequal",
        [0x25CA] = "lozenge",
        [0xFB01] = "fi",
        [0xFB02] = "fl",
    };

    private static Dictionary<string, BuiltInFontData.Entry> BuildEntryIndex()
    {
        var map = new Dictionary<string, BuiltInFontData.Entry>(StringComparer.Ordinal);
        foreach (var entry in BuiltInFontData.Fonts)
        {
            map[entry.FontName] = entry;
        }

        return map;
    }

    private static Dictionary<string, string> BuildKernIndex()
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var (fontName, pairs) in BuiltInFontKernData.Fonts)
        {
            map[fontName] = pairs;
        }

        return map;
    }

    private static Dictionary<int, int> ParseKerning(string fontName)
    {
        var map = new Dictionary<int, int>();
        if (!KernByName.TryGetValue(fontName, out var pairs) || pairs.Length == 0)
        {
            return map;
        }

        foreach (var triple in pairs.Split('|'))
        {
            var parts = triple.Split(' ');
            var left = int.Parse(parts[0], CultureInfo.InvariantCulture);
            var right = int.Parse(parts[1], CultureInfo.InvariantCulture);
            var value = int.Parse(parts[2], CultureInfo.InvariantCulture);
            map[FontMetric.PairKey(left, right)] = value;
        }

        return map;
    }
}
