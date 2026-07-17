using System;
using System.Collections.Generic;
using System.Globalization;

namespace Radzen.Documents.Pdf.Fonts;

// WinAnsiEncoding per ISO 32000-1 Annex D.2; code 160 (nbsp) named "space" per Annex D.
internal static class WinAnsiEncoding
{
    private const string Notdef = ".notdef";

    private static readonly string[] Names =
    [
        Notdef, Notdef, Notdef, Notdef, Notdef, Notdef, Notdef, Notdef,
        Notdef, Notdef, Notdef, Notdef, Notdef, Notdef, Notdef, Notdef,
        Notdef, Notdef, Notdef, Notdef, Notdef, Notdef, Notdef, Notdef,
        Notdef, Notdef, Notdef, Notdef, Notdef, Notdef, Notdef, Notdef,
        "space", "exclam", "quotedbl", "numbersign", "dollar", "percent", "ampersand", "quotesingle",
        "parenleft", "parenright", "asterisk", "plus", "comma", "hyphen", "period", "slash",
        "zero", "one", "two", "three", "four", "five", "six", "seven",
        "eight", "nine", "colon", "semicolon", "less", "equal", "greater", "question",
        "at", "A", "B", "C", "D", "E", "F", "G",
        "H", "I", "J", "K", "L", "M", "N", "O",
        "P", "Q", "R", "S", "T", "U", "V", "W",
        "X", "Y", "Z", "bracketleft", "backslash", "bracketright", "asciicircum", "underscore",
        "grave", "a", "b", "c", "d", "e", "f", "g",
        "h", "i", "j", "k", "l", "m", "n", "o",
        "p", "q", "r", "s", "t", "u", "v", "w",
        "x", "y", "z", "braceleft", "bar", "braceright", "asciitilde", Notdef,
        "Euro", Notdef, "quotesinglbase", "florin", "quotedblbase", "ellipsis", "dagger", "daggerdbl",
        "circumflex", "perthousand", "Scaron", "guilsinglleft", "OE", Notdef, "Zcaron", Notdef,
        Notdef, "quoteleft", "quoteright", "quotedblleft", "quotedblright", "bullet", "endash", "emdash",
        "tilde", "trademark", "scaron", "guilsinglright", "oe", Notdef, "zcaron", "Ydieresis",
        "space", "exclamdown", "cent", "sterling", "currency", "yen", "brokenbar", "section",
        "dieresis", "copyright", "ordfeminine", "guillemotleft", "logicalnot", "hyphen", "registered", "macron",
        "degree", "plusminus", "twosuperior", "threesuperior", "acute", "mu", "paragraph", "periodcentered",
        "cedilla", "onesuperior", "ordmasculine", "guillemotright", "onequarter", "onehalf", "threequarters", "questiondown",
        "Agrave", "Aacute", "Acircumflex", "Atilde", "Adieresis", "Aring", "AE", "Ccedilla",
        "Egrave", "Eacute", "Ecircumflex", "Edieresis", "Igrave", "Iacute", "Icircumflex", "Idieresis",
        "Eth", "Ntilde", "Ograve", "Oacute", "Ocircumflex", "Otilde", "Odieresis", "multiply",
        "Oslash", "Ugrave", "Uacute", "Ucircumflex", "Udieresis", "Yacute", "Thorn", "germandbls",
        "agrave", "aacute", "acircumflex", "atilde", "adieresis", "aring", "ae", "ccedilla",
        "egrave", "eacute", "ecircumflex", "edieresis", "igrave", "iacute", "icircumflex", "idieresis",
        "eth", "ntilde", "ograve", "oacute", "ocircumflex", "otilde", "odieresis", "divide",
        "oslash", "ugrave", "uacute", "ucircumflex", "udieresis", "yacute", "thorn", "ydieresis",
    ];

    private static readonly ushort[] CodePoints =
    [
        0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000,
        0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000,
        0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000,
        0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000,
        0x0020, 0x0021, 0x0022, 0x0023, 0x0024, 0x0025, 0x0026, 0x0027,
        0x0028, 0x0029, 0x002A, 0x002B, 0x002C, 0x002D, 0x002E, 0x002F,
        0x0030, 0x0031, 0x0032, 0x0033, 0x0034, 0x0035, 0x0036, 0x0037,
        0x0038, 0x0039, 0x003A, 0x003B, 0x003C, 0x003D, 0x003E, 0x003F,
        0x0040, 0x0041, 0x0042, 0x0043, 0x0044, 0x0045, 0x0046, 0x0047,
        0x0048, 0x0049, 0x004A, 0x004B, 0x004C, 0x004D, 0x004E, 0x004F,
        0x0050, 0x0051, 0x0052, 0x0053, 0x0054, 0x0055, 0x0056, 0x0057,
        0x0058, 0x0059, 0x005A, 0x005B, 0x005C, 0x005D, 0x005E, 0x005F,
        0x0060, 0x0061, 0x0062, 0x0063, 0x0064, 0x0065, 0x0066, 0x0067,
        0x0068, 0x0069, 0x006A, 0x006B, 0x006C, 0x006D, 0x006E, 0x006F,
        0x0070, 0x0071, 0x0072, 0x0073, 0x0074, 0x0075, 0x0076, 0x0077,
        0x0078, 0x0079, 0x007A, 0x007B, 0x007C, 0x007D, 0x007E, 0x0000,
        0x20AC, 0x0000, 0x201A, 0x0192, 0x201E, 0x2026, 0x2020, 0x2021,
        0x02C6, 0x2030, 0x0160, 0x2039, 0x0152, 0x0000, 0x017D, 0x0000,
        0x0000, 0x2018, 0x2019, 0x201C, 0x201D, 0x2022, 0x2013, 0x2014,
        0x02DC, 0x2122, 0x0161, 0x203A, 0x0153, 0x0000, 0x017E, 0x0178,
        0x00A0, 0x00A1, 0x00A2, 0x00A3, 0x00A4, 0x00A5, 0x00A6, 0x00A7,
        0x00A8, 0x00A9, 0x00AA, 0x00AB, 0x00AC, 0x00AD, 0x00AE, 0x00AF,
        0x00B0, 0x00B1, 0x00B2, 0x00B3, 0x00B4, 0x00B5, 0x00B6, 0x00B7,
        0x00B8, 0x00B9, 0x00BA, 0x00BB, 0x00BC, 0x00BD, 0x00BE, 0x00BF,
        0x00C0, 0x00C1, 0x00C2, 0x00C3, 0x00C4, 0x00C5, 0x00C6, 0x00C7,
        0x00C8, 0x00C9, 0x00CA, 0x00CB, 0x00CC, 0x00CD, 0x00CE, 0x00CF,
        0x00D0, 0x00D1, 0x00D2, 0x00D3, 0x00D4, 0x00D5, 0x00D6, 0x00D7,
        0x00D8, 0x00D9, 0x00DA, 0x00DB, 0x00DC, 0x00DD, 0x00DE, 0x00DF,
        0x00E0, 0x00E1, 0x00E2, 0x00E3, 0x00E4, 0x00E5, 0x00E6, 0x00E7,
        0x00E8, 0x00E9, 0x00EA, 0x00EB, 0x00EC, 0x00ED, 0x00EE, 0x00EF,
        0x00F0, 0x00F1, 0x00F2, 0x00F3, 0x00F4, 0x00F5, 0x00F6, 0x00F7,
        0x00F8, 0x00F9, 0x00FA, 0x00FB, 0x00FC, 0x00FD, 0x00FE, 0x00FF,
    ];

    private static readonly Dictionary<char, byte> CharToCode = BuildReverseMap();

    private static readonly Dictionary<string, int> NameToCodePoint = BuildNameMap();

    private static Dictionary<char, byte> BuildReverseMap()
    {
        var map = new Dictionary<char, byte>(256);
        for (var code = 32; code < CodePoints.Length; code++)
        {
            var cp = CodePoints[code];
            if (cp != 0)
            {
                map[(char)cp] = (byte)code;
            }
        }

        return map;
    }

    private static Dictionary<string, int> BuildNameMap()
    {
        var map = new Dictionary<string, int>(256, StringComparer.Ordinal);
        for (var code = 0; code < Names.Length; code++)
        {
            var name = Names[code];
            var cp = CodePoints[code];
            if (cp != 0 && !map.ContainsKey(name))
            {
                map[name] = cp;
            }
        }

        return map;
    }

    public static bool TryGetCodePointByName(string name, out int codePoint)
    {
        if (NameToCodePoint.TryGetValue(name, out codePoint))
        {
            return true;
        }

        if (name.Length == 7 && name.StartsWith("uni", StringComparison.Ordinal)
            && int.TryParse(name.AsSpan(3), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var uni))
        {
            codePoint = uni;
            return true;
        }

        if (name.Length is >= 5 and <= 7 && name[0] == 'u'
            && int.TryParse(name.AsSpan(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var u))
        {
            codePoint = u;
            return true;
        }

        codePoint = 0;
        return false;
    }

    public static bool TryGetChar(byte code, out char c)
    {
        var cp = CodePoints[code];
        c = (char)cp;
        return cp != 0;
    }

    public static bool TryGetCode(char c, out byte code) => CharToCode.TryGetValue(c, out code);

    public static bool CanEncode(char c) => CharToCode.ContainsKey(c);

    public static string GetGlyphName(byte code) => Names[code];
}
