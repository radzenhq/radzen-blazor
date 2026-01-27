using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Radzen.Blazor;

/// <summary>
/// Represents a rectangle used for barcode rendering, with position and size.
/// </summary>
public readonly struct BarcodeRect(double x, double y, double width, double height)
{
    /// <summary>
    /// The X position of the rectangle.
    /// </summary>
    public readonly double X = x;
    /// <summary>
    /// The Y position of the rectangle.
    /// </summary>
    public readonly double Y = y;
    /// <summary>
    /// The width of the rectangle.
    /// </summary>
    public readonly double Width = width;
    /// <summary>
    /// The height of the rectangle.
    /// </summary>
    public readonly double Height = height;
}

/// <summary>
/// Provides 1D barcode encoding utilities for common symbologies.
/// </summary>
public static class RadzenBarcodeEncoder
{
    // Code 128 patterns (0..106). Each entry is 6 digits (bar/space/bar/space/bar/space) module widths.
    // Stop code (106) is 7 digits in the spec (includes a final bar). We keep it as 7 digits and handle it.
    static readonly string[] Code128Patterns = new[]
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

        // Code 128 subset B supports ASCII 32..127 (inclusive). We treat 127 as DEL.
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

            // In Code128B, code value is ascii - 32
            codes.Add(ascii - 32);
        }

        int checksumValue = codes[0];
        for (int i = 1; i < codes.Count; i++)
        {
            checksumValue += codes[i] * i;
        }
        checksumValue %= 103;

        // expose checksum (0..102)
        checksum = checksumValue;

        codes.Add(checksumValue);
        codes.Add(stop);

        // Convert codes to module pattern widths, alternating bar/space.
        // Most codes are 6 digits; stop is 7 digits.
        var modules = new List<int>(codes.Count * 6);
        foreach (var code in codes)
        {
            var p = Code128Patterns[code];
            for (int i = 0; i < p.Length; i++)
            {
                modules.Add(p[i] - '0');
            }
        }

        // Code 128 requires a 2-module termination bar after the stop pattern.
        // Many pattern tables omit it because it can be represented by extending the
        // final bar of the stop pattern by 2 modules.
        if (modules.Count > 0)
        {
            modules[^1] += 2;
        }

        return modules;
    }

    static readonly Dictionary<char, string> Code39Map = new Dictionary<char, string>()
    {
        // Each pattern is 9 elements (bar/space alternating, starting with bar).
        // 'n' = narrow (1), 'w' = wide (2). We expand to digits.
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
        ['*'] = "nwnnwnwnn", // start/stop
    };

    /// <summary>
    /// Encodes a string into Code 39 module widths.
    /// </summary>
    /// <param name="value">The value to encode.</param>
    /// <returns>The module widths (bar/space alternating, starting with bar).</returns>
    public static IReadOnlyList<int> EncodeCode39(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        // Code39 traditionally uses uppercase
        var text = value.ToUpperInvariant();

        foreach (var ch in text)
        {
            if (!Code39Map.ContainsKey(ch))
            {
                throw new ArgumentException($"Code39 does not support character '{ch}'.");
            }
        }

        // Start + data + stop, inter-character gap (narrow space) between characters.
        var full = "*" + text + "*";
        var modules = new List<int>(full.Length * 10);

        for (int idx = 0; idx < full.Length; idx++)
        {
            var pat = Code39Map[full[idx]];
            for (int i = 0; i < pat.Length; i++)
            {
                modules.Add(pat[i] == 'w' ? 2 : 1);
            }

            // Inter-character gap (narrow space) except after last char.
            if (idx != full.Length - 1)
            {
                modules.Add(1);
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
            // pad with leading zero (common behavior)
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

        // Start: narrow bar, narrow space, narrow bar, narrow space  (1010)
        widths.Add(narrow);
        widths.Add(narrow);
        widths.Add(narrow);
        widths.Add(narrow);

        for (int i = 0; i < digits.Length; i += 2)
        {
            var a = Pat(digits[i]);
            var b = Pat(digits[i + 1]);

            for (int j = 0; j < 5; j++)
            {
                widths.Add(a[j] == 'w' ? wide : narrow); // bar
                widths.Add(b[j] == 'w' ? wide : narrow); // space
            }
        }

        // Stop: wide bar, narrow space, narrow bar (1101)
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
        // Wikipedia table mapping (bars: 1=wide, spaces: 0=wide) for the standard symbol set.
        // Ensure start/stop are present; default to A ... B if missing.
        var raw = (value ?? string.Empty).Trim().ToUpperInvariant();
        if (raw.Length == 0)
        {
            throw new ArgumentException("Codabar requires a non-empty value.");
        }

        bool HasStartStop(string s)
        {
            if (s.Length < 2) return false;
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

            // Bars: 4 bits, 1=wide
            int BarWidth(int pos) => barBits[pos] == '1' ? wide : narrow;
            // Spaces: 3 bits, 0=wide (per wikipedia mapping table)
            int SpaceWidth(int pos) => spaceBits[pos] == '0' ? wide : narrow;

            widths.Add(BarWidth(0));
            widths.Add(SpaceWidth(0));
            widths.Add(BarWidth(1));
            widths.Add(SpaceWidth(1));
            widths.Add(BarWidth(2));
            widths.Add(SpaceWidth(2));
            widths.Add(BarWidth(3));

            // Inter-character narrow space (except after last char).
            if (idx != text.Length - 1)
            {
                widths.Add(narrow);
            }
        }

        return widths;
    }

    static readonly string[] EanL = new[]
    {
        "0001101","0011001","0010011","0111101","0100011","0110001","0101111","0111011","0110111","0001011"
    };
    static readonly string[] EanG = new[]
    {
        "0100111","0110011","0011011","0100001","0011101","0111001","0000101","0010001","0001001","0010111"
    };
    static readonly string[] EanR = new[]
    {
        "1110010","1100110","1101100","1000010","1011100","1001110","1010000","1000100","1001000","1110100"
    };
    static readonly string[] Ean13Parity = new[]
    {
        "LLLLLL","LLGLGG","LLGGLG","LLGGGL","LGLLGG","LGGLLG","LGGGLL","LGLGLG","LGLGGL","LGGLGL"
    };

    static int ComputeEanCheckDigit(string digitsWithoutCheck)
    {
        // digitsWithoutCheck is 7/11/12 digits depending on symbology.
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
        // digits 2..7 (index 1..6)
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
            // ISBN-10 -> EAN-13: 978 + first 9 digits + EAN check
            var core = raw[..9];
            if (!core.All(char.IsDigit)) throw new ArgumentException("Invalid ISBN-10.");
            return EncodeEan13("978" + core, out checksumText);
        }
        if (raw.Length == 13)
        {
            if (!raw.All(char.IsDigit)) throw new ArgumentException("Invalid ISBN-13.");
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
        // ISSN EAN-13: 977 + first 7 digits + 00 + EAN check
        var raw = new string(value.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
        if (raw.Length != 8) throw new ArgumentException("ISSN requires 8 characters.");
        var core = raw[..7];
        if (!core.All(char.IsDigit)) throw new ArgumentException("Invalid ISSN.");
        return EncodeEan13("977" + core + "00", out checksumText);
    }

    /// <summary>
    /// Encodes a Pharmacode value and returns the bar geometry.
    /// </summary>
    /// <param name="value">The Pharmacode numeric value.</param>
    /// <param name="barHeight">The bar height in SVG units.</param>
    /// <param name="quietZone">The quiet zone in modules.</param>
    /// <returns>The bar rectangles and viewBox width.</returns>
    public static (IReadOnlyList<BarcodeRect> bars, double vbWidth) EncodePharmacode(string value, double barHeight, int quietZone)
    {
        ArgumentNullException.ThrowIfNull(value);

        // Pharmacode one-track: numbers 3..131070
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

        double x = Math.Max(0, quietZone);
        var rects = new List<BarcodeRect>(bars.Count);
        foreach (var b in bars)
        {
            rects.Add(new BarcodeRect(x, 0, b.width, barHeight));
            x += b.width + 1; // 1 module gap
        }

        var vbWidth = x + Math.Max(0, quietZone);
        if (vbWidth <= 0) vbWidth = 1;
        return (rects, vbWidth);
    }

    // POSTNET digit encoding from Wikipedia (weights 7,4,2,1,0). 1=full bar, 0=half bar.
    static readonly Dictionary<char, string> PostnetDigitBits = new Dictionary<char, string>()
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
    public static (IReadOnlyList<BarcodeRect> bars, double vbWidth) EncodePostnet(string value, double barHeight, int quietZone, out string checksumText)
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

        double fullH = barHeight;
        double halfH = barHeight / 2.0;
        double halfY = fullH - halfH;

        double x = Math.Max(0, quietZone);
        var rects = new List<BarcodeRect>();

        // Start frame bar (full)
        rects.Add(new BarcodeRect(x, 0, 1, fullH));
        x += 2; // bar(1) + space(1)

        foreach (var ch in payload)
        {
            var bits = PostnetDigitBits[ch];
            for (int i = 0; i < 5; i++)
            {
                bool full = bits[i] == '1';
                rects.Add(full
                    ? new BarcodeRect(x, 0, 1, fullH)
                    : new BarcodeRect(x, halfY, 1, halfH));
                x += 2;
            }
        }

        // Stop frame bar (full)
        rects.Add(new BarcodeRect(x, 0, 1, fullH));
        x += 1;

        var vbWidth = x + Math.Max(0, quietZone);
        if (vbWidth <= 0) vbWidth = 1;
        return (rects, vbWidth);
    }

    // RM4SCC patterns and symbol matrix from Wikipedia:
    // Top patterns (values 1..6) and Bottom patterns (values 1..6) are:
    // 1=0011, 2=0101, 3=0110, 4=1001, 5=1010, 6=1100
    static readonly string[] Rm4Patterns = new[] { "0011", "0101", "0110", "1001", "1010", "1100" };

    // Matrix indexed by [topValue-1, bottomValue-1]
    static readonly char[,] Rm4Matrix = new char[6, 6]
    {
        { '0', '1', '2', '3', '4', '5' },
        { '6', '7', '8', '9', 'A', 'B' },
        { 'C', 'D', 'E', 'F', 'G', 'H' },
        { 'I', 'J', 'K', 'L', 'M', 'N' },
        { 'O', 'P', 'Q', 'R', 'S', 'T' },
        { 'U', 'V', 'W', 'X', 'Y', 'Z' }
    };

    static readonly Dictionary<char, (string top, string bottom)> Rm4CharToBits = BuildRm4CharToBits();

    static Dictionary<char, (string top, string bottom)> BuildRm4CharToBits()
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
    public static (IReadOnlyList<BarcodeRect> bars, double vbWidth) EncodeRm4scc(string value, double barHeight, int quietZone, out string checksumText)
    {
        ArgumentNullException.ThrowIfNull(value);

        var text = new string(value.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
        if (string.IsNullOrEmpty(text))
        {
            throw new ArgumentException("RM4SCC requires alphanumeric input.");
        }

        // Only symbols present in the Wikipedia table (0-9, A-Z).
        foreach (var ch in text)
        {
            if (!(ch is >= '0' and <= '9') && !(ch is >= 'A' and <= 'Z'))
            {
                throw new ArgumentException($"RM4SCC does not support character '{ch}'.");
            }
        }

        // Compute checksum per Wikipedia: sum top values and bottom values separately, mod 6, 0 => 6.
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

        // Encode start + data + checksum + stop.
        // Start/stop are single bars; we use ascender for start and descender for stop.
        double h = barHeight;
        double third = h / 3.0;
        double trackerY = third;
        double trackerH = third;

        BarcodeRect BarRect(double x, bool top, bool bottom)
        {
            return (top, bottom) switch
            {
                (false, false) => new BarcodeRect(x, trackerY, 1, trackerH), // tracker
                (true, false) => new BarcodeRect(x, 0, 1, trackerY + trackerH), // ascender (top + tracker)
                (false, true) => new BarcodeRect(x, trackerY, 1, h - trackerY), // descender (tracker + bottom)
                (true, true) => new BarcodeRect(x, 0, 1, h), // full
            };
        }

        double xPos = Math.Max(0, quietZone);
        var rects = new List<BarcodeRect>();

        // Start bar (ascender)
        rects.Add(BarRect(xPos, top: true, bottom: false));
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

        foreach (var ch in text) AddSymbol(ch);
        AddSymbol(checkChar);

        // Stop bar (descender)
        rects.Add(BarRect(xPos, top: false, bottom: true));
        xPos += 1;

        var vbWidth = xPos + Math.Max(0, quietZone);
        if (vbWidth <= 0) vbWidth = 1;
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
        if (digits.Length == 0) throw new ArgumentException("Plessey (MSI) requires numeric input.");

        // Mod 10 (Luhn) check digit (common)
        int check = ComputeLuhnCheckDigit(digits);
        checksumText = check.ToString(CultureInfo.InvariantCulture);
        digits += checksumText;

        // MSI map from Wikipedia.
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

        var sb = new StringBuilder();
        sb.Append("110"); // start
        foreach (var ch in digits) sb.Append(DigitMap(ch));
        sb.Append("1001"); // stop
        return sb.ToString();
    }

    static int ComputeLuhnCheckDigit(string digits)
    {
        int sum = 0;
        bool dbl = true;
        for (int i = digits.Length - 1; i >= 0; i--)
        {
            int d = digits[i] - '0';
            if (dbl)
            {
                d *= 2;
                if (d > 9) d -= 9;
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
        // Telepen algorithm per Wikipedia: even parity bytes, little-endian bit order, modulo-127 checksum.
        var bytes = Encoding.ASCII.GetBytes(value ?? string.Empty);

        int sum = 0;
        for (int i = 0; i < bytes.Length; i++)
        {
            if (bytes[i] > 0x7F) throw new ArgumentException("Telepen supports ASCII only.");
            sum = (sum + bytes[i]) % 127;
        }

        int check = (127 - sum) % 127;
        checksumText = check.ToString(CultureInfo.InvariantCulture);

        // Build payload: start '_' + data + checksum byte + stop 'z'
        var payload = new List<byte>(bytes.Length + 3) { (byte)'_' };
        payload.AddRange(bytes);
        payload.Add((byte)check);
        payload.Add((byte)'z');

        // Build bit stream LSB-first with even parity bit as MSB.
        var bitStream = new List<int>(payload.Count * 8);
        foreach (var b0 in payload)
        {
            int b = b0 & 0x7F;
            int ones = CountBits(b);
            int parityBit = (ones % 2 == 0) ? 0 : 1; // make total even
            int byteWithParity = b | (parityBit << 7);

            for (int i = 0; i < 8; i++)
            {
                bitStream.Add((byteWithParity >> i) & 1);
            }
        }

        // Encode bit stream into bar/space widths (narrow=1, wide=3).
        // We produce alternating bar/space widths list, starting with bar.
        const int narrow = 1;
        const int wide = 3;
        var widths = new List<int>();

        int idx = 0;
        while (idx < bitStream.Count)
        {
            if (bitStream[idx] == 1)
            {
                // "1" => narrow bar, narrow space
                widths.Add(narrow);
                widths.Add(narrow);
                idx += 1;
                continue;
            }

            // starts with 0
            if (idx + 1 < bitStream.Count && bitStream[idx + 1] == 0)
            {
                // "00" => wide bar, narrow space
                widths.Add(wide);
                widths.Add(narrow);
                idx += 2;
                continue;
            }

            if (idx + 2 < bitStream.Count && bitStream[idx] == 0 && bitStream[idx + 1] == 1 && bitStream[idx + 2] == 0)
            {
                // "010" => wide bar, wide space
                widths.Add(wide);
                widths.Add(wide);
                idx += 3;
                continue;
            }

            // General block 0 1^k 0 with k>=2
            if (idx + 3 >= bitStream.Count || bitStream[idx] != 0 || bitStream[idx + 1] != 1)
            {
                throw new ArgumentException("Invalid Telepen bit stream.");
            }

            int j = idx + 1;
            while (j < bitStream.Count && bitStream[j] == 1) j++;
            if (j >= bitStream.Count || bitStream[j] != 0)
            {
                throw new ArgumentException("Invalid Telepen bit stream.");
            }

            int k = j - (idx + 1); // number of 1s
            if (k < 2) throw new ArgumentException("Invalid Telepen bit stream.");

            // leading "01" => narrow bar, wide space
            widths.Add(narrow);
            widths.Add(wide);

            // middle extra 1s (k-2) => narrow bar, narrow space
            for (int m = 0; m < k - 2; m++)
            {
                widths.Add(narrow);
                widths.Add(narrow);
            }

            // trailing "10" => narrow bar, wide space
            widths.Add(narrow);
            widths.Add(wide);

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
    public static string ToSvg(RadzenBarcodeType type, string value, double barHeight = 50, int quietZoneModules = 10, string foreground = "#000000", string background = "#FFFFFF")
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(foreground);
        ArgumentNullException.ThrowIfNull(background);

        var (bars, viewBoxWidth, viewBoxHeight) = EncodeToBars(type, value, barHeight, quietZoneModules);

        if (viewBoxWidth <= 0) viewBoxWidth = 1;
        if (viewBoxHeight <= 0) viewBoxHeight = 1;

        var sb = new StringBuilder(bars.Count * 64 + 256);
        sb.Append(CultureInfo.InvariantCulture, $"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{F(viewBoxWidth)}\" height=\"{F(viewBoxHeight)}\" viewBox=\"0 0 {F(viewBoxWidth)} {F(viewBoxHeight)}\" shape-rendering=\"crispEdges\">");
        sb.Append(CultureInfo.InvariantCulture, $"<rect x=\"0\" y=\"0\" width=\"{F(viewBoxWidth)}\" height=\"{F(viewBoxHeight)}\" fill=\"{background}\"/>");

        for (int i = 0; i < bars.Count; i++)
        {
            var bar = bars[i];
            sb.Append(CultureInfo.InvariantCulture, $"<rect x=\"{F(bar.X)}\" y=\"{F(bar.Y)}\" width=\"{F(bar.Width)}\" height=\"{F(bar.Height)}\" fill=\"{foreground}\"/>");
        }

        sb.Append("</svg>");
        return sb.ToString();
    }

    static (IReadOnlyList<BarcodeRect> bars, double vbWidth, double vbHeight) EncodeToBars(RadzenBarcodeType type, string value, double barHeight, int quietZoneModules)
    {
        switch (type)
        {
            case RadzenBarcodeType.Code128:
            {
                var widths = EncodeCode128B(value, out _);
                var (bars, vbWidth) = CreateFromWidths(widths, barHeight, quietZoneModules);
                return (bars, vbWidth, ComputeBarsHeight(bars, barHeight));
            }
            case RadzenBarcodeType.Code39:
            {
                var widths = EncodeCode39(value);
                var (bars, vbWidth) = CreateFromWidths(widths, barHeight, quietZoneModules);
                return (bars, vbWidth, ComputeBarsHeight(bars, barHeight));
            }
            case RadzenBarcodeType.Codabar:
            {
                var widths = EncodeCodabar(value);
                var (bars, vbWidth) = CreateFromWidths(widths, barHeight, quietZoneModules);
                return (bars, vbWidth, ComputeBarsHeight(bars, barHeight));
            }
            case RadzenBarcodeType.Itf:
            {
                var widths = EncodeItf(value);
                var (bars, vbWidth) = CreateFromWidths(widths, barHeight, quietZoneModules);
                return (bars, vbWidth, ComputeBarsHeight(bars, barHeight));
            }
            case RadzenBarcodeType.Ean13:
            {
                var bits = EncodeEan13(value, out _);
                var (bars, vbWidth) = CreateFromBits(bits, barHeight, quietZoneModules);
                return (bars, vbWidth, ComputeBarsHeight(bars, barHeight));
            }
            case RadzenBarcodeType.Ean8:
            {
                var bits = EncodeEan8(value, out _);
                var (bars, vbWidth) = CreateFromBits(bits, barHeight, quietZoneModules);
                return (bars, vbWidth, ComputeBarsHeight(bars, barHeight));
            }
            case RadzenBarcodeType.UpcA:
            {
                var bits = EncodeUpcA(value, out _);
                var (bars, vbWidth) = CreateFromBits(bits, barHeight, quietZoneModules);
                return (bars, vbWidth, ComputeBarsHeight(bars, barHeight));
            }
            case RadzenBarcodeType.Isbn:
            {
                var bits = EncodeIsbnAsEan13(value, out _);
                var (bars, vbWidth) = CreateFromBits(bits, barHeight, quietZoneModules);
                return (bars, vbWidth, ComputeBarsHeight(bars, barHeight));
            }
            case RadzenBarcodeType.Issn:
            {
                var bits = EncodeIssnAsEan13(value, out _);
                var (bars, vbWidth) = CreateFromBits(bits, barHeight, quietZoneModules);
                return (bars, vbWidth, ComputeBarsHeight(bars, barHeight));
            }
            case RadzenBarcodeType.Pharmacode:
            {
                var geometry = EncodePharmacode(value, barHeight, quietZoneModules);
                var (bars, vbWidth) = CreateFromRects(geometry);
                return (bars, vbWidth, ComputeBarsHeight(bars, barHeight));
            }
            case RadzenBarcodeType.Postnet:
            {
                var geometry = EncodePostnet(value, barHeight, quietZoneModules, out _);
                var (bars, vbWidth) = CreateFromRects(geometry);
                return (bars, vbWidth, ComputeBarsHeight(bars, barHeight));
            }
            case RadzenBarcodeType.Rm4scc:
            {
                var geometry = EncodeRm4scc(value, barHeight, quietZoneModules, out _);
                var (bars, vbWidth) = CreateFromRects(geometry);
                return (bars, vbWidth, ComputeBarsHeight(bars, barHeight));
            }
            case RadzenBarcodeType.Msi:
            {
                var bits = EncodeMsiPlessey(value, out _);
                var (bars, vbWidth) = CreateFromBits(bits, barHeight, quietZoneModules);
                return (bars, vbWidth, ComputeBarsHeight(bars, barHeight));
            }
            case RadzenBarcodeType.Telepen:
            {
                var widths = EncodeTelepen(value, out _);
                var (bars, vbWidth) = CreateFromWidths(widths, barHeight, quietZoneModules);
                return (bars, vbWidth, ComputeBarsHeight(bars, barHeight));
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, "Unsupported barcode type.");
        }
    }

    static (IReadOnlyList<BarcodeRect> bars, double vbWidth) CreateFromWidths(IReadOnlyList<int> widths, double barHeight, int quietZoneModules)
    {
        ArgumentNullException.ThrowIfNull(widths);

        var rects = new List<BarcodeRect>();
        double x = Math.Max(0, quietZoneModules);
        bool isBar = true;
        for (int i = 0; i < widths.Count; i++)
        {
            var w = widths[i];
            if (isBar && w > 0)
            {
                rects.Add(new BarcodeRect(x, 0, w, barHeight));
            }
            x += w;
            isBar = !isBar;
        }

        var vbWidth = x + Math.Max(0, quietZoneModules);
        if (vbWidth <= 0) vbWidth = 1;
        return (rects, vbWidth);
    }

    static (IReadOnlyList<BarcodeRect> bars, double vbWidth) CreateFromBits(string bits, double barHeight, int quietZoneModules)
    {
        ArgumentNullException.ThrowIfNull(bits);

        if (bits.Length == 0)
        {
            return (Array.Empty<BarcodeRect>(), 1);
        }

        var rects = new List<BarcodeRect>();
        var quiet = Math.Max(0, quietZoneModules);
        for (int i = 0; i < bits.Length;)
        {
            if (bits[i] != '1')
            {
                i++;
                continue;
            }

            int j = i + 1;
            while (j < bits.Length && bits[j] == '1') j++;
            rects.Add(new BarcodeRect(quiet + i, 0, j - i, barHeight));
            i = j;
        }

        var vbWidth = quiet + bits.Length + quiet;
        if (vbWidth <= 0) vbWidth = 1;
        return (rects, vbWidth);
    }

    static (IReadOnlyList<BarcodeRect> bars, double vbWidth) CreateFromRects((IReadOnlyList<BarcodeRect> bars, double vbWidth) geometry)
    {
        var vbWidth = geometry.vbWidth;
        if (vbWidth <= 0) vbWidth = 1;
        return (geometry.bars, vbWidth);
    }

    static double ComputeBarsHeight(IReadOnlyList<BarcodeRect> bars, double fallbackHeight)
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

    static int CountBits(int v)
    {
        int c = 0;
        while (v != 0)
        {
            c += v & 1;
            v >>= 1;
        }
        return c;
    }

    static string F(double v) => v.ToString(CultureInfo.InvariantCulture);
}
