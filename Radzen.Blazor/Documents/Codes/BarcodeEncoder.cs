using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Radzen.Documents.Codes;

/// <summary>
/// Barcode type.
/// </summary>
public enum BarcodeType
{
    /// <summary>
    /// Code 128 (subset B). Encodes ASCII text and is widely supported.
    /// </summary>
    Code128,

    /// <summary>
    /// Code 39. Supports uppercase letters, digits, and a limited set of symbols.
    /// </summary>
    Code39,

    /// <summary>EAN-13 (GTIN-13).</summary>
    Ean13,

    /// <summary>EAN-8 (GTIN-8).</summary>
    Ean8,

    /// <summary>UPC-A.</summary>
    UpcA,

    /// <summary>ITF (Interleaved 2 of 5).</summary>
    Itf,

    /// <summary>POSTNET (USPS). 4/5-state style using short/long bars.</summary>
    Postnet,

    /// <summary>RM4SCC (Royal Mail 4-state customer code).</summary>
    Rm4scc,

    /// <summary>Codabar.</summary>
    Codabar,

    /// <summary>Pharmacode (one-track).</summary>
    Pharmacode,

    /// <summary>ISBN (encodes as EAN-13 with 978/979 prefix).</summary>
    Isbn,

    /// <summary>ISSN (encodes as EAN-13 with 977 prefix).</summary>
    Issn,

    /// <summary>
    /// MSI (Modified Plessey). Numeric-only barcode, commonly used for inventory.
    /// </summary>
    Msi,

    /// <summary>Telepen.</summary>
    Telepen
}

/// <summary>
/// Provides 1D barcode encoding utilities for common symbologies.
/// </summary>
public static class BarcodeEncoder
{
    // ISO/IEC 15417 - Code 128 symbol character element widths; the stop character (106) has a seventh element.
    private static readonly string[] Code128Patterns = new[]
    {
        "212222","222122","222221","121223","121322","131222","122213","122312","132212","221213",
        "221312","231212","112232","122132","122231","113222","123122","123221","223211","221132",
        "221231","213212","223112","312131","311222","321122","321221","312212","322112","322211",
        "212123","212321","232121","111323","131123","131321","112313","132113","132311","211313",
        "231113","231311","112133","112331","132131","113123","113321","133121","313121","211331",
        "231131","213113","213311","213131","311123","311321","331121","312113","312311","332111",
        "314111","221411","431111","111224","111422","121124","121421","141122","141221","112214",
        "112412","122114","122411","142112","142211","241211","221114","413111","241112","134111",
        "111242","121142","121241","114212","124112","124211","411212","421112","421211","212141",
        "214121","412121","111143","111341","131141","114113","114311","411113","411311","113141",
        "114131","311141","411131","211412","211214","211232","2331112"
    };

    /// <summary>
    /// Encodes a string into Code 128 subset B module widths.
    /// </summary>
    /// <param name="value">The value to encode.</param>
    /// <returns>The module widths (bar/space alternating, starting with bar).</returns>
    public static IReadOnlyList<int> EncodeCode128B(string value) => EncodeCode128B(value, out _);

    /// <summary>
    /// Encodes a string into Code 128 subset B module widths and returns the checksum.
    /// </summary>
    /// <param name="value">The value to encode.</param>
    /// <param name="checksum">The calculated checksum value.</param>
    /// <returns>The module widths (bar/space alternating, starting with bar).</returns>
    public static IReadOnlyList<int> EncodeCode128B(string value, out int checksum)
    {
        ArgumentNullException.ThrowIfNull(value);

        // ISO/IEC 15417 - Code Set B encodes ASCII 32..127.
        var codes = new List<int>(value.Length + 3);
        const int startB = 104;
        const int stop = 106;

        codes.Add(startB);

        for (int i = 0; i < value.Length; i++)
        {
            var ch = value[i];
            int ascii = ch;
            if (ascii < 32 || ascii > 127)
            {
                throw new ArgumentException($"Code128B supports ASCII 32..127. Invalid character: U+{ascii:X4}.");
            }

            // ISO/IEC 15417 - the Code Set B value is the ASCII code minus 32.
            codes.Add(ascii - 32);
        }

        int checksumValue = codes[0];
        for (int i = 1; i < codes.Count; i++)
        {
            checksumValue += codes[i] * i;
        }
        checksumValue %= 103;

        checksum = checksumValue;

        codes.Add(checksumValue);
        codes.Add(stop);

        var modules = new List<int>(codes.Count * 6);
        foreach (var code in codes)
        {
            var p = Code128Patterns[code];
            for (int i = 0; i < p.Length; i++)
            {
                modules.Add(p[i] - '0');
            }
        }

        // ISO/IEC 15417 - the stop character is followed by a 2-module termination bar.
        if (modules.Count > 0)
        {
            modules[^1] += 2;
        }

        return modules;
    }

    private static readonly Dictionary<char, string> Code39Map = new Dictionary<char, string>()
    {
        // ISO/IEC 16388 - nine alternating elements starting with a bar, narrow ('n') or wide ('w').
        ['0'] = "nnnwwnwnn",
        ['1'] = "wnnwnnnnw",
        ['2'] = "nnwwnnnnw",
        ['3'] = "wnwwnnnnn",
        ['4'] = "nnnwwnnnw",
        ['5'] = "wnnwwnnnn",
        ['6'] = "nnwwwnnnn",
        ['7'] = "nnnwnnwnw",
        ['8'] = "wnnwnnwnn",
        ['9'] = "nnwwnnwnn",
        ['A'] = "wnnnnwnnw",
        ['B'] = "nnwnnwnnw",
        ['C'] = "wnwnnwnnn",
        ['D'] = "nnnnwwnnw",
        ['E'] = "wnnnwwnnn",
        ['F'] = "nnwnwwnnn",
        ['G'] = "nnnnnwwnw",
        ['H'] = "wnnnnwwnn",
        ['I'] = "nnwnnwwnn",
        ['J'] = "nnnnwwwnn",
        ['K'] = "wnnnnnnww",
        ['L'] = "nnwnnnnww",
        ['M'] = "wnwnnnnwn",
        ['N'] = "nnnnwnnww",
        ['O'] = "wnnnwnnwn",
        ['P'] = "nnwnwnnwn",
        ['Q'] = "nnnnnnwww",
        ['R'] = "wnnnnnwwn",
        ['S'] = "nnwnnnwwn",
        ['T'] = "nnnnwnwwn",
        ['U'] = "wwnnnnnnw",
        ['V'] = "nwwnnnnnw",
        ['W'] = "wwwnnnnnn",
        ['X'] = "nwnnwnnnw",
        ['Y'] = "wwnnwnnnn",
        ['Z'] = "nwwnwnnnn",
        ['-'] = "nwnnnnwnw",
        ['.'] = "wwnnnnwnn",
        [' '] = "nwwnnnwnn",
        ['$'] = "nwnwnwnnn",
        ['/'] = "nwnwnnnwn",
        ['+'] = "nwnnnwnwn",
        ['%'] = "nnnwnwnwn",
        ['*'] = "nwnnwnwnn",
    };

    /// <summary>
    /// Encodes a string into Code 39 module widths.
    /// </summary>
    /// <param name="value">The value to encode.</param>
    /// <returns>The module widths (bar/space alternating, starting with bar).</returns>
    public static IReadOnlyList<int> EncodeCode39(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var text = value.ToUpperInvariant();

        foreach (var ch in text)
        {
            if (!Code39Map.ContainsKey(ch))
            {
                throw new ArgumentException($"Code39 does not support character '{ch}'.");
            }
        }

        const int narrow = 1;
        const int wide = 2;
        const string startStop = "*";

        var full = startStop + text + startStop;
        var modules = new List<int>(full.Length * 10);

        for (int idx = 0; idx < full.Length; idx++)
        {
            var pat = Code39Map[full[idx]];
            for (int i = 0; i < pat.Length; i++)
            {
                modules.Add(pat[i] == 'w' ? wide : narrow);
            }

            bool isLastCharacter = idx == full.Length - 1;
            if (!isLastCharacter)
            {
                modules.Add(narrow);
            }
        }

        return modules;
    }

    /// <summary>
    /// Encodes a string into ITF (Interleaved 2 of 5) module widths.
    /// </summary>
    /// <param name="value">The value to encode.</param>
    /// <returns>The module widths (bar/space alternating, starting with bar).</returns>
    public static IReadOnlyList<int> EncodeItf(string value)
    {
        var digits = new string(value.Where(char.IsDigit).ToArray());
        if (digits.Length == 0)
        {
            throw new ArgumentException("ITF requires numeric input.");
        }
        if (digits.Length % 2 != 0)
        {
            digits = "0" + digits;
        }

        const int narrow = 1;
        const int wide = 3;

        static string Pat(char d) => d switch
        {
            '0' => "nnwwn",
            '1' => "wnnnw",
            '2' => "nwnnw",
            '3' => "wwnnn",
            '4' => "nnwnw",
            '5' => "wnwnn",
            '6' => "nwwnn",
            '7' => "nnnww",
            '8' => "wnnwn",
            '9' => "nwnwn",
            _ => throw new ArgumentException("ITF requires numeric input.")
        };

        var widths = new List<int>(digits.Length * 10 + 16);

        // ISO/IEC 16390 - the start pattern is four narrow elements.
        widths.Add(narrow);
        widths.Add(narrow);
        widths.Add(narrow);
        widths.Add(narrow);

        for (int i = 0; i < digits.Length; i += 2)
        {
            var barPattern = Pat(digits[i]);
            var spacePattern = Pat(digits[i + 1]);

            for (int j = 0; j < 5; j++)
            {
                widths.Add(barPattern[j] == 'w' ? wide : narrow);
                widths.Add(spacePattern[j] == 'w' ? wide : narrow);
            }
        }

        // ISO/IEC 16390 - the stop pattern is a wide bar, a narrow space and a narrow bar.
        widths.Add(wide);
        widths.Add(narrow);
        widths.Add(narrow);

        return widths;
    }

    /// <summary>
    /// Encodes a string into Codabar module widths.
    /// </summary>
    /// <param name="value">The value to encode.</param>
    /// <returns>The module widths (bar/space alternating, starting with bar).</returns>
    public static IReadOnlyList<int> EncodeCodabar(string value)
    {
        var raw = (value ?? string.Empty).Trim().ToUpperInvariant();
        if (raw.Length == 0)
        {
            throw new ArgumentException("Codabar requires a non-empty value.");
        }

        bool HasStartStop(string s)
        {
            if (s.Length < 2)
            {
                return false;
            }

            bool isStart = s[0] == 'A' || s[0] == 'B' || s[0] == 'C' || s[0] == 'D';
            bool isStop = s[s.Length - 1] == 'A' || s[s.Length - 1] == 'B' || s[s.Length - 1] == 'C' || s[s.Length - 1] == 'D';
            return isStart && isStop;
        }

        var text = HasStartStop(raw) ? raw : $"A{raw}B";

        static (string spaceBits, string barBits) Map(char ch) => ch switch
        {
            '0' => ("001", "0001"),
            '1' => ("001", "0010"),
            '2' => ("010", "0001"),
            '3' => ("100", "1000"),
            '4' => ("001", "0100"),
            '5' => ("001", "1000"),
            '6' => ("100", "0001"),
            '7' => ("100", "0010"),
            '8' => ("100", "0100"),
            '9' => ("010", "1000"),
            '-' => ("010", "0010"),
            '$' => ("010", "0100"),
            '.' => ("000", "0001"),
            '/' => ("000", "0010"),
            ':' => ("000", "0100"),
            '+' => ("000", "1000"),
            'A' => ("011", "0100"),
            'B' => ("110", "0001"),
            'C' => ("011", "0001"),
            'D' => ("011", "0010"),
            _ => throw new ArgumentException($"Codabar does not support character '{ch}'.")
        };

        const int narrow = 1;
        const int wide = 3;

        var widths = new List<int>(text.Length * 8);

        for (int idx = 0; idx < text.Length; idx++)
        {
            var ch = text[idx];
            var (spaceBits, barBits) = Map(ch);

            int BarWidth(int pos) => barBits[pos] == '1' ? wide : narrow;
            int SpaceWidth(int pos) => spaceBits[pos] == '0' ? wide : narrow;

            widths.Add(BarWidth(0));
            widths.Add(SpaceWidth(0));
            widths.Add(BarWidth(1));
            widths.Add(SpaceWidth(1));
            widths.Add(BarWidth(2));
            widths.Add(SpaceWidth(2));
            widths.Add(BarWidth(3));

            bool isLastCharacter = idx == text.Length - 1;
            if (!isLastCharacter)
            {
                widths.Add(narrow);
            }
        }

        return widths;
    }

    private static readonly string[] EanL = new[]
    {
        "0001101","0011001","0010011","0111101","0100011","0110001","0101111","0111011","0110111","0001011"
    };
    private static readonly string[] EanG = new[]
    {
        "0100111","0110011","0011011","0100001","0011101","0111001","0000101","0010001","0001001","0010111"
    };
    private static readonly string[] EanR = new[]
    {
        "1110010","1100110","1101100","1000010","1011100","1001110","1010000","1000100","1001000","1110100"
    };
    private static readonly string[] Ean13Parity = new[]
    {
        "LLLLLL","LLGLGG","LLGGLG","LLGGGL","LGLLGG","LGGLLG","LGGGLL","LGLGLG","LGLGGL","LGGLGL"
    };

    private static int ComputeEanCheckDigit(string digitsWithoutCheck)
    {
        int sum = 0;
        bool weight3 = true;
        for (int i = digitsWithoutCheck.Length - 1; i >= 0; i--)
        {
            int d = digitsWithoutCheck[i] - '0';
            sum += weight3 ? d * 3 : d;
            weight3 = !weight3;
        }
        int mod = sum % 10;
        return (10 - mod) % 10;
    }

    /// <summary>
    /// Encodes a string into EAN-13 bit pattern.
    /// </summary>
    /// <param name="value">The value to encode.</param>
    /// <param name="checksumText">The calculated checksum digit.</param>
    /// <returns>The bit pattern (1=bar, 0=space).</returns>
    public static string EncodeEan13(string value, out string checksumText)
    {
        var digits = new string(value.Where(char.IsDigit).ToArray());
        if (digits.Length != 12 && digits.Length != 13)
        {
            throw new ArgumentException("EAN-13 requires 12 or 13 digits.");
        }

        if (digits.Length == 12)
        {
            var check = ComputeEanCheckDigit(digits);
            digits += check.ToString(CultureInfo.InvariantCulture);
        }
        else
        {
            var expected = ComputeEanCheckDigit(digits[..12]);
            if (digits[12] - '0' != expected)
            {
                throw new ArgumentException("Invalid EAN-13 check digit.");
            }
        }

        checksumText = digits[^1].ToString();

        int first = digits[0] - '0';
        var parity = Ean13Parity[first];

        var sb = new StringBuilder(95);
        sb.Append("101");
        for (int i = 1; i <= 6; i++)
        {
            int d = digits[i] - '0';
            sb.Append(parity[i - 1] == 'G' ? EanG[d] : EanL[d]);
        }
        sb.Append("01010");
        for (int i = 7; i <= 12; i++)
        {
            int d = digits[i] - '0';
            sb.Append(EanR[d]);
        }
        sb.Append("101");
        return sb.ToString();
    }

    /// <summary>
    /// Encodes a string into UPC-A bit pattern.
    /// </summary>
    /// <param name="value">The value to encode.</param>
    /// <param name="checksumText">The calculated checksum digit.</param>
    /// <returns>The bit pattern (1=bar, 0=space).</returns>
    public static string EncodeUpcA(string value, out string checksumText)
    {
        var digits = new string(value.Where(char.IsDigit).ToArray());
        if (digits.Length != 11 && digits.Length != 12)
        {
            throw new ArgumentException("UPC-A requires 11 or 12 digits.");
        }

        if (digits.Length == 11)
        {
            var check = ComputeEanCheckDigit(digits);
            digits += check.ToString(CultureInfo.InvariantCulture);
        }
        else
        {
            var expected = ComputeEanCheckDigit(digits[..11]);
            if (digits[11] - '0' != expected)
            {
                throw new ArgumentException("Invalid UPC-A check digit.");
            }
        }

        checksumText = digits[^1].ToString();

        var sb = new StringBuilder(95);
        sb.Append("101");
        for (int i = 0; i < 6; i++)
        {
            int d = digits[i] - '0';
            sb.Append(EanL[d]);
        }
        sb.Append("01010");
        for (int i = 6; i < 12; i++)
        {
            int d = digits[i] - '0';
            sb.Append(EanR[d]);
        }
        sb.Append("101");
        return sb.ToString();
    }

    /// <summary>
    /// Encodes a string into EAN-8 bit pattern.
    /// </summary>
    /// <param name="value">The value to encode.</param>
    /// <param name="checksumText">The calculated checksum digit.</param>
    /// <returns>The bit pattern (1=bar, 0=space).</returns>
    public static string EncodeEan8(string value, out string checksumText)
    {
        var digits = new string(value.Where(char.IsDigit).ToArray());
        if (digits.Length != 7 && digits.Length != 8)
        {
            throw new ArgumentException("EAN-8 requires 7 or 8 digits.");
        }

        if (digits.Length == 7)
        {
            var check = ComputeEanCheckDigit(digits);
            digits += check.ToString(CultureInfo.InvariantCulture);
        }
        else
        {
            var expected = ComputeEanCheckDigit(digits[..7]);
            if (digits[7] - '0' != expected)
            {
                throw new ArgumentException("Invalid EAN-8 check digit.");
            }
        }

        checksumText = digits[^1].ToString();

        var sb = new StringBuilder(67);
        sb.Append("101");
        for (int i = 0; i < 4; i++)
        {
            int d = digits[i] - '0';
            sb.Append(EanL[d]);
        }
        sb.Append("01010");
        for (int i = 4; i < 8; i++)
        {
            int d = digits[i] - '0';
            sb.Append(EanR[d]);
        }
        sb.Append("101");
        return sb.ToString();
    }

    /// <summary>
    /// Encodes an ISBN as EAN-13 bit pattern.
    /// </summary>
    /// <param name="value">The ISBN value to encode.</param>
    /// <param name="checksumText">The calculated checksum digit.</param>
    /// <returns>The bit pattern (1=bar, 0=space).</returns>
    public static string EncodeIsbnAsEan13(string value, out string checksumText)
    {
        var raw = new string(value.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
        if (raw.Length == 10)
        {
            var core = raw[..9];
            if (!core.All(char.IsDigit))
            {
                throw new ArgumentException("Invalid ISBN-10.");
            }

            return EncodeEan13("978" + core, out checksumText);
        }
        if (raw.Length == 13)
        {
            if (!raw.All(char.IsDigit))
            {
                throw new ArgumentException("Invalid ISBN-13.");
            }

            return EncodeEan13(raw, out checksumText);
        }

        throw new ArgumentException("ISBN requires 10 or 13 characters.");
    }

    /// <summary>
    /// Encodes an ISSN as EAN-13 bit pattern.
    /// </summary>
    /// <param name="value">The ISSN value to encode.</param>
    /// <param name="checksumText">The calculated checksum digit.</param>
    /// <returns>The bit pattern (1=bar, 0=space).</returns>
    public static string EncodeIssnAsEan13(string value, out string checksumText)
    {
        var raw = new string(value.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
        if (raw.Length != 8)
        {
            throw new ArgumentException("ISSN requires 8 characters.");
        }

        var core = raw[..7];
        if (!core.All(char.IsDigit))
        {
            throw new ArgumentException("Invalid ISSN.");
        }

        return EncodeEan13("977" + core + "00", out checksumText);
    }

    /// <summary>
    /// Encodes a Pharmacode value and returns the bar geometry.
    /// </summary>
    /// <param name="value">The Pharmacode numeric value.</param>
    /// <param name="barHeight">The bar height in SVG units.</param>
    /// <param name="quietZone">The quiet zone in modules.</param>
    /// <returns>The bar rectangles and viewBox width.</returns>
    public static (IReadOnlyList<Rect> bars, double vbWidth) EncodePharmacode(string value, double barHeight, int quietZone)
    {
        ArgumentNullException.ThrowIfNull(value);

        var digits = new string(value.Where(char.IsDigit).ToArray());
        if (!int.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out var n))
        {
            throw new ArgumentException("Pharmacode requires a numeric value.");
        }
        if (n < 3 || n > 131070)
        {
            throw new ArgumentException("Pharmacode value must be in range 3..131070.");
        }

        var bars = new List<(int width, bool isWide)>();
        while (n > 0)
        {
            if (n % 2 == 0)
            {
                bars.Add((2, true));
                n = (n - 2) / 2;
            }
            else
            {
                bars.Add((1, false));
                n = (n - 1) / 2;
            }
        }
        bars.Reverse();

        const int gapModules = 1;

        double x = Math.Max(0, quietZone);
        var rects = new List<Rect>(bars.Count);
        foreach (var b in bars)
        {
            rects.Add(new Rect(x, 0, b.width, barHeight));
            x += b.width + gapModules;
        }

        var vbWidth = x + Math.Max(0, quietZone);
        if (vbWidth <= 0)
        {
            vbWidth = 1;
        }

        return (rects, vbWidth);
    }

    private static readonly Dictionary<char, string> PostnetDigitBits = new Dictionary<char, string>()
    {
        ['0'] = "11000",
        ['1'] = "00011",
        ['2'] = "00101",
        ['3'] = "00110",
        ['4'] = "01001",
        ['5'] = "01010",
        ['6'] = "01100",
        ['7'] = "10001",
        ['8'] = "10010",
        ['9'] = "10100",
    };

    /// <summary>
    /// Encodes a POSTNET value and returns the bar geometry.
    /// </summary>
    /// <param name="value">The value to encode.</param>
    /// <param name="barHeight">The bar height in SVG units.</param>
    /// <param name="quietZone">The quiet zone in modules.</param>
    /// <param name="checksumText">The calculated checksum digit.</param>
    /// <returns>The bar rectangles and viewBox width.</returns>
    public static (IReadOnlyList<Rect> bars, double vbWidth) EncodePostnet(string value, double barHeight, int quietZone, out string checksumText)
    {
        ArgumentNullException.ThrowIfNull(value);

        var digits = new string(value.Where(char.IsDigit).ToArray());
        if ((digits.Length != 5 && digits.Length != 9 && digits.Length != 11))
        {
            throw new ArgumentException("POSTNET requires 5, 9, or 11 digits (ZIP / ZIP+4 / Delivery Point).");
        }

        int sum = digits.Sum(ch => ch - '0');
        int check = (10 - (sum % 10)) % 10;
        checksumText = check.ToString(CultureInfo.InvariantCulture);

        var payload = digits + checksumText;

        const int barPitch = 2;
        const int barWidth = 1;

        double fullH = barHeight;
        double halfH = barHeight / 2.0;
        double halfY = fullH - halfH;

        Rect FullBar(double at) => new Rect(at, 0, barWidth, fullH);
        Rect HalfBar(double at) => new Rect(at, halfY, barWidth, halfH);

        double x = Math.Max(0, quietZone);
        var rects = new List<Rect>();

        rects.Add(FullBar(x));
        x += barPitch;

        foreach (var ch in payload)
        {
            var bits = PostnetDigitBits[ch];
            for (int i = 0; i < 5; i++)
            {
                bool full = bits[i] == '1';
                rects.Add(full ? FullBar(x) : HalfBar(x));
                x += barPitch;
            }
        }

        rects.Add(FullBar(x));
        x += barWidth;

        var vbWidth = x + Math.Max(0, quietZone);
        if (vbWidth <= 0)
        {
            vbWidth = 1;
        }

        return (rects, vbWidth);
    }

    private static readonly string[] Rm4Patterns = new[] { "0011", "0101", "0110", "1001", "1010", "1100" };

    private static readonly char[,] Rm4Matrix = new char[6, 6]
    {
        { '0', '1', '2', '3', '4', '5' },
        { '6', '7', '8', '9', 'A', 'B' },
        { 'C', 'D', 'E', 'F', 'G', 'H' },
        { 'I', 'J', 'K', 'L', 'M', 'N' },
        { 'O', 'P', 'Q', 'R', 'S', 'T' },
        { 'U', 'V', 'W', 'X', 'Y', 'Z' }
    };

    private static readonly Dictionary<char, (string top, string bottom)> Rm4CharToBits = BuildRm4CharToBits();

    private static Dictionary<char, (string top, string bottom)> BuildRm4CharToBits()
    {
        var dict = new Dictionary<char, (string top, string bottom)>();
        for (int r = 0; r < 6; r++)
        {
            for (int c = 0; c < 6; c++)
            {
                dict[Rm4Matrix[r, c]] = (Rm4Patterns[r], Rm4Patterns[c]);
            }
        }
        return dict;
    }

    /// <summary>
    /// Encodes a RM4SCC value and returns the bar geometry.
    /// </summary>
    /// <param name="value">The value to encode.</param>
    /// <param name="barHeight">The bar height in SVG units.</param>
    /// <param name="quietZone">The quiet zone in modules.</param>
    /// <param name="checksumText">The calculated checksum character.</param>
    /// <returns>The bar rectangles and viewBox width.</returns>
    public static (IReadOnlyList<Rect> bars, double vbWidth) EncodeRm4scc(string value, double barHeight, int quietZone, out string checksumText)
    {
        ArgumentNullException.ThrowIfNull(value);

        var text = new string(value.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
        if (string.IsNullOrEmpty(text))
        {
            throw new ArgumentException("RM4SCC requires alphanumeric input.");
        }

        foreach (var ch in text)
        {
            if (!(ch is >= '0' and <= '9') && !(ch is >= 'A' and <= 'Z'))
            {
                throw new ArgumentException($"RM4SCC does not support character '{ch}'.");
            }
        }

        int sumTop = 0;
        int sumBottom = 0;
        foreach (var ch in text)
        {
            if (!Rm4CharToBits.TryGetValue(ch, out var bits))
            {
                throw new ArgumentException($"RM4SCC does not support character '{ch}'.");
            }
            int t = Array.IndexOf(Rm4Patterns, bits.top) + 1;
            int b = Array.IndexOf(Rm4Patterns, bits.bottom) + 1;
            sumTop += t;
            sumBottom += b;
        }

        int topVal = sumTop % 6;
        topVal = topVal == 0 ? 6 : topVal;
        int bottomVal = sumBottom % 6;
        bottomVal = bottomVal == 0 ? 6 : bottomVal;

        var checkChar = Rm4Matrix[topVal - 1, bottomVal - 1];

        checksumText = checkChar.ToString();

        double h = barHeight;
        double third = h / 3.0;
        double trackerY = third;
        double trackerH = third;

        Rect Tracker(double x) => new Rect(x, trackerY, 1, trackerH);
        Rect Ascender(double x) => new Rect(x, 0, 1, trackerY + trackerH);
        Rect Descender(double x) => new Rect(x, trackerY, 1, h - trackerY);
        Rect FullBar(double x) => new Rect(x, 0, 1, h);

        Rect BarRect(double x, bool top, bool bottom)
        {
            return (top, bottom) switch
            {
                (false, false) => Tracker(x),
                (true, false) => Ascender(x),
                (false, true) => Descender(x),
                (true, true) => FullBar(x),
            };
        }

        double xPos = Math.Max(0, quietZone);
        var rects = new List<Rect>();

        rects.Add(Ascender(xPos));
        xPos += 2;

        void AddSymbol(char ch)
        {
            var (topBits, bottomBits) = Rm4CharToBits[ch];
            for (int i = 0; i < 4; i++)
            {
                bool top = topBits[i] == '1';
                bool bottom = bottomBits[i] == '1';
                rects.Add(BarRect(xPos, top, bottom));
                xPos += 2;
            }
        }

        foreach (var ch in text)
        {
            AddSymbol(ch);
        }

        AddSymbol(checkChar);

        rects.Add(Descender(xPos));
        xPos += 1;

        var vbWidth = xPos + Math.Max(0, quietZone);
        if (vbWidth <= 0)
        {
            vbWidth = 1;
        }

        return (rects, vbWidth);
    }

    /// <summary>
    /// Encodes a value into MSI (Modified Plessey) bit pattern.
    /// </summary>
    /// <param name="value">The value to encode.</param>
    /// <param name="checksumText">The calculated checksum digit.</param>
    /// <returns>The bit pattern (1=bar, 0=space).</returns>
    public static string EncodeMsiPlessey(string value, out string checksumText)
    {
        var digits = new string(value.Where(char.IsDigit).ToArray());
        if (digits.Length == 0)
        {
            throw new ArgumentException("Plessey (MSI) requires numeric input.");
        }

        int check = ComputeLuhnCheckDigit(digits);
        checksumText = check.ToString(CultureInfo.InvariantCulture);
        digits += checksumText;

        static string DigitMap(char d) => d switch
        {
            '0' => "100100100100",
            '1' => "100100100110",
            '2' => "100100110100",
            '3' => "100100110110",
            '4' => "100110100100",
            '5' => "100110100110",
            '6' => "100110110100",
            '7' => "100110110110",
            '8' => "110100100100",
            '9' => "110100100110",
            _ => throw new ArgumentException("MSI requires numeric input.")
        };

        const string startPattern = "110";
        const string stopPattern = "1001";

        var sb = new StringBuilder();
        sb.Append(startPattern);
        foreach (var ch in digits)
        {
            sb.Append(DigitMap(ch));
        }

        sb.Append(stopPattern);
        return sb.ToString();
    }

    private static int ComputeLuhnCheckDigit(string digits)
    {
        int sum = 0;
        bool dbl = true;
        for (int i = digits.Length - 1; i >= 0; i--)
        {
            int d = digits[i] - '0';
            if (dbl)
            {
                d *= 2;
                if (d > 9)
                {
                    d -= 9;
                }
            }
            sum += d;
            dbl = !dbl;
        }
        return (10 - (sum % 10)) % 10;
    }

    /// <summary>
    /// Encodes a string into Telepen module widths.
    /// </summary>
    /// <param name="value">The value to encode.</param>
    /// <param name="checksumText">The calculated checksum value.</param>
    /// <returns>The module widths (bar/space alternating, starting with bar).</returns>
    public static IReadOnlyList<int> EncodeTelepen(string value, out string checksumText)
    {
        var bytes = Encoding.ASCII.GetBytes(value ?? string.Empty);

        int sum = 0;
        for (int i = 0; i < bytes.Length; i++)
        {
            if (bytes[i] > 0x7F)
            {
                throw new ArgumentException("Telepen supports ASCII only.");
            }

            sum = (sum + bytes[i]) % 127;
        }

        int check = (127 - sum) % 127;
        checksumText = check.ToString(CultureInfo.InvariantCulture);

        var payload = new List<byte>(bytes.Length + 3) { (byte)'_' };
        payload.AddRange(bytes);
        payload.Add((byte)check);
        payload.Add((byte)'z');

        var bitStream = new List<int>(payload.Count * 8);
        foreach (var b0 in payload)
        {
            int b = b0 & 0x7F;
            int ones = CountBits(b);
            int evenParityBit = (ones % 2 == 0) ? 0 : 1;
            int byteWithParity = b | (evenParityBit << 7);

            for (int i = 0; i < 8; i++)
            {
                bitStream.Add((byteWithParity >> i) & 1);
            }
        }

        const int narrow = 1;
        const int wide = 3;
        var widths = new List<int>();

        void AddNarrowBarNarrowSpace() { widths.Add(narrow); widths.Add(narrow); }
        void AddNarrowBarWideSpace() { widths.Add(narrow); widths.Add(wide); }
        void AddWideBarNarrowSpace() { widths.Add(wide); widths.Add(narrow); }
        void AddWideBarWideSpace() { widths.Add(wide); widths.Add(wide); }

        int idx = 0;
        while (idx < bitStream.Count)
        {
            if (bitStream[idx] == 1)
            {
                AddNarrowBarNarrowSpace();
                idx += 1;
                continue;
            }

            if (idx + 1 < bitStream.Count && bitStream[idx + 1] == 0)
            {
                AddWideBarNarrowSpace();
                idx += 2;
                continue;
            }

            if (idx + 2 < bitStream.Count && bitStream[idx] == 0 && bitStream[idx + 1] == 1 && bitStream[idx + 2] == 0)
            {
                AddWideBarWideSpace();
                idx += 3;
                continue;
            }

            if (idx + 3 >= bitStream.Count || bitStream[idx] != 0 || bitStream[idx + 1] != 1)
            {
                throw new ArgumentException("Invalid Telepen bit stream.");
            }

            int j = idx + 1;
            while (j < bitStream.Count && bitStream[j] == 1)
            {
                j++;
            }

            if (j >= bitStream.Count || bitStream[j] != 0)
            {
                throw new ArgumentException("Invalid Telepen bit stream.");
            }

            int oneCount = j - (idx + 1);
            if (oneCount < 2)
            {
                throw new ArgumentException("Invalid Telepen bit stream.");
            }

            AddNarrowBarWideSpace();

            for (int m = 0; m < oneCount - 2; m++)
            {
                AddNarrowBarNarrowSpace();
            }

            AddNarrowBarWideSpace();

            idx = j + 1;
        }

        return widths;
    }

    /// <summary>
    /// Encodes a barcode value and renders it into an SVG string.
    /// </summary>
    /// <param name="type">The barcode type.</param>
    /// <param name="value">The value to encode.</param>
    /// <param name="barHeight">The bar height in SVG units.</param>
    /// <param name="quietZoneModules">The quiet zone in modules.</param>
    /// <param name="foreground">The bar color.</param>
    /// <param name="background">The background color.</param>
    /// <returns>An SVG string representing the barcode.</returns>
    /// <exception cref="ArgumentException"><paramref name="foreground"/> or <paramref name="background"/> is not a valid CSS color.</exception>
    public static string ToSvg(BarcodeType type, string value, double barHeight = 50, int quietZoneModules = 10, string foreground = "#000000", string background = "#FFFFFF")
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(foreground);
        ArgumentNullException.ThrowIfNull(background);

        var barFill = SvgAttributes.Color(foreground, nameof(foreground));
        var backgroundFill = SvgAttributes.Color(background, nameof(background));

        var (bars, viewBoxWidth, viewBoxHeight) = EncodeToBars(type, value, barHeight, quietZoneModules);

        if (viewBoxWidth <= 0)
        {
            viewBoxWidth = 1;
        }

        if (viewBoxHeight <= 0)
        {
            viewBoxHeight = 1;
        }

        var sb = new StringBuilder(bars.Count * 64 + 256);
        sb.Append(CultureInfo.InvariantCulture, $"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{F(viewBoxWidth)}\" height=\"{F(viewBoxHeight)}\" viewBox=\"0 0 {F(viewBoxWidth)} {F(viewBoxHeight)}\" shape-rendering=\"crispEdges\">");
        sb.Append(CultureInfo.InvariantCulture, $"<rect x=\"0\" y=\"0\" width=\"{F(viewBoxWidth)}\" height=\"{F(viewBoxHeight)}\" fill=\"{backgroundFill}\"/>");

        for (int i = 0; i < bars.Count; i++)
        {
            var bar = bars[i];
            sb.Append(CultureInfo.InvariantCulture, $"<rect x=\"{F(bar.X)}\" y=\"{F(bar.Y)}\" width=\"{F(bar.Width)}\" height=\"{F(bar.Height)}\" fill=\"{barFill}\"/>");
        }

        sb.Append("</svg>");
        return sb.ToString();
    }

    internal static (IReadOnlyList<Rect> bars, double vbWidth, double vbHeight) EncodeToBars(BarcodeType type, string value, double barHeight, int quietZoneModules)
    {
        switch (type)
        {
            case BarcodeType.Code128:
            {
                var widths = EncodeCode128B(value, out _);
                var (bars, vbWidth) = CreateFromWidths(widths, barHeight, quietZoneModules);
                return (bars, vbWidth, ComputeBarsHeight(bars, barHeight));
            }
            case BarcodeType.Code39:
            {
                var widths = EncodeCode39(value);
                var (bars, vbWidth) = CreateFromWidths(widths, barHeight, quietZoneModules);
                return (bars, vbWidth, ComputeBarsHeight(bars, barHeight));
            }
            case BarcodeType.Codabar:
            {
                var widths = EncodeCodabar(value);
                var (bars, vbWidth) = CreateFromWidths(widths, barHeight, quietZoneModules);
                return (bars, vbWidth, ComputeBarsHeight(bars, barHeight));
            }
            case BarcodeType.Itf:
            {
                var widths = EncodeItf(value);
                var (bars, vbWidth) = CreateFromWidths(widths, barHeight, quietZoneModules);
                return (bars, vbWidth, ComputeBarsHeight(bars, barHeight));
            }
            case BarcodeType.Ean13:
            {
                var bits = EncodeEan13(value, out _);
                var (bars, vbWidth) = CreateFromBits(bits, barHeight, quietZoneModules);
                return (bars, vbWidth, ComputeBarsHeight(bars, barHeight));
            }
            case BarcodeType.Ean8:
            {
                var bits = EncodeEan8(value, out _);
                var (bars, vbWidth) = CreateFromBits(bits, barHeight, quietZoneModules);
                return (bars, vbWidth, ComputeBarsHeight(bars, barHeight));
            }
            case BarcodeType.UpcA:
            {
                var bits = EncodeUpcA(value, out _);
                var (bars, vbWidth) = CreateFromBits(bits, barHeight, quietZoneModules);
                return (bars, vbWidth, ComputeBarsHeight(bars, barHeight));
            }
            case BarcodeType.Isbn:
            {
                var bits = EncodeIsbnAsEan13(value, out _);
                var (bars, vbWidth) = CreateFromBits(bits, barHeight, quietZoneModules);
                return (bars, vbWidth, ComputeBarsHeight(bars, barHeight));
            }
            case BarcodeType.Issn:
            {
                var bits = EncodeIssnAsEan13(value, out _);
                var (bars, vbWidth) = CreateFromBits(bits, barHeight, quietZoneModules);
                return (bars, vbWidth, ComputeBarsHeight(bars, barHeight));
            }
            case BarcodeType.Pharmacode:
            {
                var geometry = EncodePharmacode(value, barHeight, quietZoneModules);
                var (bars, vbWidth) = CreateFromRects(geometry);
                return (bars, vbWidth, ComputeBarsHeight(bars, barHeight));
            }
            case BarcodeType.Postnet:
            {
                var geometry = EncodePostnet(value, barHeight, quietZoneModules, out _);
                var (bars, vbWidth) = CreateFromRects(geometry);
                return (bars, vbWidth, ComputeBarsHeight(bars, barHeight));
            }
            case BarcodeType.Rm4scc:
            {
                var geometry = EncodeRm4scc(value, barHeight, quietZoneModules, out _);
                var (bars, vbWidth) = CreateFromRects(geometry);
                return (bars, vbWidth, ComputeBarsHeight(bars, barHeight));
            }
            case BarcodeType.Msi:
            {
                var bits = EncodeMsiPlessey(value, out _);
                var (bars, vbWidth) = CreateFromBits(bits, barHeight, quietZoneModules);
                return (bars, vbWidth, ComputeBarsHeight(bars, barHeight));
            }
            case BarcodeType.Telepen:
            {
                var widths = EncodeTelepen(value, out _);
                var (bars, vbWidth) = CreateFromWidths(widths, barHeight, quietZoneModules);
                return (bars, vbWidth, ComputeBarsHeight(bars, barHeight));
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, "Unsupported barcode type.");
        }
    }

    private static (IReadOnlyList<Rect> bars, double vbWidth) CreateFromWidths(IReadOnlyList<int> widths, double barHeight, int quietZoneModules)
    {
        ArgumentNullException.ThrowIfNull(widths);

        var rects = new List<Rect>();
        double x = Math.Max(0, quietZoneModules);
        bool isBar = true;
        for (int i = 0; i < widths.Count; i++)
        {
            var w = widths[i];
            if (isBar && w > 0)
            {
                rects.Add(new Rect(x, 0, w, barHeight));
            }
            x += w;
            isBar = !isBar;
        }

        var vbWidth = x + Math.Max(0, quietZoneModules);
        if (vbWidth <= 0)
        {
            vbWidth = 1;
        }

        return (rects, vbWidth);
    }

    private static (IReadOnlyList<Rect> bars, double vbWidth) CreateFromBits(string bits, double barHeight, int quietZoneModules)
    {
        ArgumentNullException.ThrowIfNull(bits);

        if (bits.Length == 0)
        {
            return (Array.Empty<Rect>(), 1);
        }

        var rects = new List<Rect>();
        var quiet = Math.Max(0, quietZoneModules);
        for (int i = 0; i < bits.Length;)
        {
            if (bits[i] != '1')
            {
                i++;
                continue;
            }

            int j = i + 1;
            while (j < bits.Length && bits[j] == '1')
            {
                j++;
            }

            rects.Add(new Rect(quiet + i, 0, j - i, barHeight));
            i = j;
        }

        var vbWidth = quiet + bits.Length + quiet;
        if (vbWidth <= 0)
        {
            vbWidth = 1;
        }

        return (rects, vbWidth);
    }

    private static (IReadOnlyList<Rect> bars, double vbWidth) CreateFromRects((IReadOnlyList<Rect> bars, double vbWidth) geometry)
    {
        var vbWidth = geometry.vbWidth;
        if (vbWidth <= 0)
        {
            vbWidth = 1;
        }

        return (geometry.bars, vbWidth);
    }

    private static double ComputeBarsHeight(IReadOnlyList<Rect> bars, double fallbackHeight)
    {
        if (bars.Count == 0)
        {
            return Math.Max(0, fallbackHeight);
        }

        double max = 0;
        for (int i = 0; i < bars.Count; i++)
        {
            var bar = bars[i];
            max = Math.Max(max, bar.Y + bar.Height);
        }
        return Math.Max(0, max);
    }

    private static int CountBits(int v)
    {
        int c = 0;
        while (v != 0)
        {
            c += v & 1;
            v >>= 1;
        }
        return c;
    }

    private static string F(double v) => v.ToString(CultureInfo.InvariantCulture);
}
