using System;
using System.Globalization;

namespace Radzen.Documents;


/// <summary>
/// Represents an RGBA color with 8 bits per channel.
/// </summary>
public readonly struct Color : IEquatable<Color>
{
    private Color(byte a, byte r, byte g, byte b)
    {
        A = a;
        R = r;
        G = g;
        B = b;
    }

    /// <summary>
    /// Gets the alpha channel (0 transparent, 255 opaque).
    /// </summary>
    public byte A { get; }

    /// <summary>
    /// Gets the red channel.
    /// </summary>
    public byte R { get; }

    /// <summary>
    /// Gets the green channel.
    /// </summary>
    public byte G { get; }

    /// <summary>
    /// Gets the blue channel.
    /// </summary>
    public byte B { get; }

    /// <summary>
    /// Creates an opaque color from the specified red, green and blue channels.
    /// </summary>
    public static Color FromRgb(byte r, byte g, byte b) => new(255, r, g, b);

    /// <summary>
    /// Creates a color from the specified alpha, red, green and blue channels.
    /// </summary>
    public static Color FromArgb(byte a, byte r, byte g, byte b) => new(a, r, g, b);

    /// <summary>
    /// Creates a color from a CSS hex string. Supports <c>#RGB</c>, <c>#RGBA</c>, <c>#RRGGBB</c> and
    /// <c>#RRGGBBAA</c>, with or without a leading <c>#</c>. The alpha channel comes last, as in
    /// CSS Color Module Level 4 section 5.2; a string without one is opaque.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="hex"/> is <see langword="null"/>.</exception>
    /// <exception cref="FormatException"><paramref name="hex"/> is not a valid hex color.</exception>
    public static Color FromHex(string hex)
    {
        ArgumentNullException.ThrowIfNull(hex);

        var value = hex.StartsWith('#') ? hex[1..] : hex;

        // CSS Color Module Level 4 section 5.2: the optional fourth component is alpha.
        return value.Length switch
        {
            3 => FromRgb(ParseNibble(value[0]), ParseNibble(value[1]), ParseNibble(value[2])),
            4 => FromArgb(ParseNibble(value[3]), ParseNibble(value[0]), ParseNibble(value[1]), ParseNibble(value[2])),
            6 => FromRgb(ParseByte(value, 0), ParseByte(value, 2), ParseByte(value, 4)),
            8 => FromArgb(ParseByte(value, 6), ParseByte(value, 0), ParseByte(value, 2), ParseByte(value, 4)),
            _ => throw new FormatException($"'{hex}' is not a valid hex color."),
        };
    }

    private static byte ParseByte(string value, int index)
        => (byte)((ParseHexDigit(value[index]) << 4) | ParseHexDigit(value[index + 1]));

    private static byte ParseNibble(char c)
    {
        var nibble = ParseHexDigit(c);
        return (byte)((nibble << 4) | nibble);
    }

    private static int ParseHexDigit(char c) => c switch
    {
        >= '0' and <= '9' => c - '0',
        >= 'a' and <= 'f' => c - 'a' + 10,
        >= 'A' and <= 'F' => c - 'A' + 10,
        _ => throw new FormatException($"'{c}' is not a valid hex digit."),
    };

    /// <summary>
    /// Creates an opaque color from a CSS color keyword, for example <c>rebeccapurple</c> or
    /// <c>DarkSlateGray</c>. Keywords are matched case-insensitively, as in CSS Color Module
    /// Level 4 section 6.1.
    /// </summary>
    /// <param name="name">The CSS color keyword.</param>
    /// <returns>The named color.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
    /// <exception cref="FormatException"><paramref name="name"/> is not a CSS color keyword.</exception>
    public static Color FromName(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        return NamedColors.TryGet(name, out var rgb)
            ? FromRgb((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb)
            : throw new FormatException($"'{name}' is not a CSS color keyword.");
    }

    /// <summary>Alice blue (#F0F8FF).</summary>
    public static Color AliceBlue => FromRgb(240, 248, 255);

    /// <summary>Antique white (#FAEBD7).</summary>
    public static Color AntiqueWhite => FromRgb(250, 235, 215);

    /// <summary>Aqua (#00FFFF).</summary>
    public static Color Aqua => FromRgb(0, 255, 255);

    /// <summary>Aquamarine (#7FFFD4).</summary>
    public static Color Aquamarine => FromRgb(127, 255, 212);

    /// <summary>Azure (#F0FFFF).</summary>
    public static Color Azure => FromRgb(240, 255, 255);

    /// <summary>Beige (#F5F5DC).</summary>
    public static Color Beige => FromRgb(245, 245, 220);

    /// <summary>Bisque (#FFE4C4).</summary>
    public static Color Bisque => FromRgb(255, 228, 196);

    /// <summary>Black (#000000).</summary>
    public static Color Black => FromRgb(0, 0, 0);

    /// <summary>Blanched almond (#FFEBCD).</summary>
    public static Color BlanchedAlmond => FromRgb(255, 235, 205);

    /// <summary>Blue (#0000FF).</summary>
    public static Color Blue => FromRgb(0, 0, 255);

    /// <summary>Blue violet (#8A2BE2).</summary>
    public static Color BlueViolet => FromRgb(138, 43, 226);

    /// <summary>Brown (#A52A2A).</summary>
    public static Color Brown => FromRgb(165, 42, 42);

    /// <summary>Burly wood (#DEB887).</summary>
    public static Color BurlyWood => FromRgb(222, 184, 135);

    /// <summary>Cadet blue (#5F9EA0).</summary>
    public static Color CadetBlue => FromRgb(95, 158, 160);

    /// <summary>Chartreuse (#7FFF00).</summary>
    public static Color Chartreuse => FromRgb(127, 255, 0);

    /// <summary>Chocolate (#D2691E).</summary>
    public static Color Chocolate => FromRgb(210, 105, 30);

    /// <summary>Coral (#FF7F50).</summary>
    public static Color Coral => FromRgb(255, 127, 80);

    /// <summary>Cornflower blue (#6495ED).</summary>
    public static Color CornflowerBlue => FromRgb(100, 149, 237);

    /// <summary>Cornsilk (#FFF8DC).</summary>
    public static Color Cornsilk => FromRgb(255, 248, 220);

    /// <summary>Crimson (#DC143C).</summary>
    public static Color Crimson => FromRgb(220, 20, 60);

    /// <summary>Cyan (#00FFFF).</summary>
    public static Color Cyan => FromRgb(0, 255, 255);

    /// <summary>Dark blue (#00008B).</summary>
    public static Color DarkBlue => FromRgb(0, 0, 139);

    /// <summary>Dark cyan (#008B8B).</summary>
    public static Color DarkCyan => FromRgb(0, 139, 139);

    /// <summary>Dark goldenrod (#B8860B).</summary>
    public static Color DarkGoldenrod => FromRgb(184, 134, 11);

    /// <summary>Dark gray (#A9A9A9).</summary>
    public static Color DarkGray => FromRgb(169, 169, 169);

    /// <summary>Dark green (#006400).</summary>
    public static Color DarkGreen => FromRgb(0, 100, 0);

    /// <summary>Dark grey (#A9A9A9).</summary>
    public static Color DarkGrey => FromRgb(169, 169, 169);

    /// <summary>Dark khaki (#BDB76B).</summary>
    public static Color DarkKhaki => FromRgb(189, 183, 107);

    /// <summary>Dark magenta (#8B008B).</summary>
    public static Color DarkMagenta => FromRgb(139, 0, 139);

    /// <summary>Dark olive green (#556B2F).</summary>
    public static Color DarkOliveGreen => FromRgb(85, 107, 47);

    /// <summary>Dark orange (#FF8C00).</summary>
    public static Color DarkOrange => FromRgb(255, 140, 0);

    /// <summary>Dark orchid (#9932CC).</summary>
    public static Color DarkOrchid => FromRgb(153, 50, 204);

    /// <summary>Dark red (#8B0000).</summary>
    public static Color DarkRed => FromRgb(139, 0, 0);

    /// <summary>Dark salmon (#E9967A).</summary>
    public static Color DarkSalmon => FromRgb(233, 150, 122);

    /// <summary>Dark sea green (#8FBC8F).</summary>
    public static Color DarkSeaGreen => FromRgb(143, 188, 143);

    /// <summary>Dark slate blue (#483D8B).</summary>
    public static Color DarkSlateBlue => FromRgb(72, 61, 139);

    /// <summary>Dark slate gray (#2F4F4F).</summary>
    public static Color DarkSlateGray => FromRgb(47, 79, 79);

    /// <summary>Dark slate grey (#2F4F4F).</summary>
    public static Color DarkSlateGrey => FromRgb(47, 79, 79);

    /// <summary>Dark turquoise (#00CED1).</summary>
    public static Color DarkTurquoise => FromRgb(0, 206, 209);

    /// <summary>Dark violet (#9400D3).</summary>
    public static Color DarkViolet => FromRgb(148, 0, 211);

    /// <summary>Deep pink (#FF1493).</summary>
    public static Color DeepPink => FromRgb(255, 20, 147);

    /// <summary>Deep sky blue (#00BFFF).</summary>
    public static Color DeepSkyBlue => FromRgb(0, 191, 255);

    /// <summary>Dim gray (#696969).</summary>
    public static Color DimGray => FromRgb(105, 105, 105);

    /// <summary>Dim grey (#696969).</summary>
    public static Color DimGrey => FromRgb(105, 105, 105);

    /// <summary>Dodger blue (#1E90FF).</summary>
    public static Color DodgerBlue => FromRgb(30, 144, 255);

    /// <summary>Fire brick (#B22222).</summary>
    public static Color FireBrick => FromRgb(178, 34, 34);

    /// <summary>Floral white (#FFFAF0).</summary>
    public static Color FloralWhite => FromRgb(255, 250, 240);

    /// <summary>Forest green (#228B22).</summary>
    public static Color ForestGreen => FromRgb(34, 139, 34);

    /// <summary>Fuchsia (#FF00FF).</summary>
    public static Color Fuchsia => FromRgb(255, 0, 255);

    /// <summary>Gainsboro (#DCDCDC).</summary>
    public static Color Gainsboro => FromRgb(220, 220, 220);

    /// <summary>Ghost white (#F8F8FF).</summary>
    public static Color GhostWhite => FromRgb(248, 248, 255);

    /// <summary>Gold (#FFD700).</summary>
    public static Color Gold => FromRgb(255, 215, 0);

    /// <summary>Goldenrod (#DAA520).</summary>
    public static Color Goldenrod => FromRgb(218, 165, 32);

    /// <summary>Gray (#808080).</summary>
    public static Color Gray => FromRgb(128, 128, 128);

    /// <summary>Green (#008000).</summary>
    public static Color Green => FromRgb(0, 128, 0);

    /// <summary>Green yellow (#ADFF2F).</summary>
    public static Color GreenYellow => FromRgb(173, 255, 47);

    /// <summary>Grey (#808080).</summary>
    public static Color Grey => FromRgb(128, 128, 128);

    /// <summary>Honeydew (#F0FFF0).</summary>
    public static Color Honeydew => FromRgb(240, 255, 240);

    /// <summary>Hot pink (#FF69B4).</summary>
    public static Color HotPink => FromRgb(255, 105, 180);

    /// <summary>Indian red (#CD5C5C).</summary>
    public static Color IndianRed => FromRgb(205, 92, 92);

    /// <summary>Indigo (#4B0082).</summary>
    public static Color Indigo => FromRgb(75, 0, 130);

    /// <summary>Ivory (#FFFFF0).</summary>
    public static Color Ivory => FromRgb(255, 255, 240);

    /// <summary>Khaki (#F0E68C).</summary>
    public static Color Khaki => FromRgb(240, 230, 140);

    /// <summary>Lavender (#E6E6FA).</summary>
    public static Color Lavender => FromRgb(230, 230, 250);

    /// <summary>Lavender blush (#FFF0F5).</summary>
    public static Color LavenderBlush => FromRgb(255, 240, 245);

    /// <summary>Lawn green (#7CFC00).</summary>
    public static Color LawnGreen => FromRgb(124, 252, 0);

    /// <summary>Lemon chiffon (#FFFACD).</summary>
    public static Color LemonChiffon => FromRgb(255, 250, 205);

    /// <summary>Light blue (#ADD8E6).</summary>
    public static Color LightBlue => FromRgb(173, 216, 230);

    /// <summary>Light coral (#F08080).</summary>
    public static Color LightCoral => FromRgb(240, 128, 128);

    /// <summary>Light cyan (#E0FFFF).</summary>
    public static Color LightCyan => FromRgb(224, 255, 255);

    /// <summary>Light goldenrod yellow (#FAFAD2).</summary>
    public static Color LightGoldenrodYellow => FromRgb(250, 250, 210);

    /// <summary>Light gray (#D3D3D3).</summary>
    public static Color LightGray => FromRgb(211, 211, 211);

    /// <summary>Light green (#90EE90).</summary>
    public static Color LightGreen => FromRgb(144, 238, 144);

    /// <summary>Light grey (#D3D3D3).</summary>
    public static Color LightGrey => FromRgb(211, 211, 211);

    /// <summary>Light pink (#FFB6C1).</summary>
    public static Color LightPink => FromRgb(255, 182, 193);

    /// <summary>Light salmon (#FFA07A).</summary>
    public static Color LightSalmon => FromRgb(255, 160, 122);

    /// <summary>Light sea green (#20B2AA).</summary>
    public static Color LightSeaGreen => FromRgb(32, 178, 170);

    /// <summary>Light sky blue (#87CEFA).</summary>
    public static Color LightSkyBlue => FromRgb(135, 206, 250);

    /// <summary>Light slate gray (#778899).</summary>
    public static Color LightSlateGray => FromRgb(119, 136, 153);

    /// <summary>Light slate grey (#778899).</summary>
    public static Color LightSlateGrey => FromRgb(119, 136, 153);

    /// <summary>Light steel blue (#B0C4DE).</summary>
    public static Color LightSteelBlue => FromRgb(176, 196, 222);

    /// <summary>Light yellow (#FFFFE0).</summary>
    public static Color LightYellow => FromRgb(255, 255, 224);

    /// <summary>Lime (#00FF00).</summary>
    public static Color Lime => FromRgb(0, 255, 0);

    /// <summary>Lime green (#32CD32).</summary>
    public static Color LimeGreen => FromRgb(50, 205, 50);

    /// <summary>Linen (#FAF0E6).</summary>
    public static Color Linen => FromRgb(250, 240, 230);

    /// <summary>Magenta (#FF00FF).</summary>
    public static Color Magenta => FromRgb(255, 0, 255);

    /// <summary>Maroon (#800000).</summary>
    public static Color Maroon => FromRgb(128, 0, 0);

    /// <summary>Medium aquamarine (#66CDAA).</summary>
    public static Color MediumAquamarine => FromRgb(102, 205, 170);

    /// <summary>Medium blue (#0000CD).</summary>
    public static Color MediumBlue => FromRgb(0, 0, 205);

    /// <summary>Medium orchid (#BA55D3).</summary>
    public static Color MediumOrchid => FromRgb(186, 85, 211);

    /// <summary>Medium purple (#9370DB).</summary>
    public static Color MediumPurple => FromRgb(147, 112, 219);

    /// <summary>Medium sea green (#3CB371).</summary>
    public static Color MediumSeaGreen => FromRgb(60, 179, 113);

    /// <summary>Medium slate blue (#7B68EE).</summary>
    public static Color MediumSlateBlue => FromRgb(123, 104, 238);

    /// <summary>Medium spring green (#00FA9A).</summary>
    public static Color MediumSpringGreen => FromRgb(0, 250, 154);

    /// <summary>Medium turquoise (#48D1CC).</summary>
    public static Color MediumTurquoise => FromRgb(72, 209, 204);

    /// <summary>Medium violet red (#C71585).</summary>
    public static Color MediumVioletRed => FromRgb(199, 21, 133);

    /// <summary>Midnight blue (#191970).</summary>
    public static Color MidnightBlue => FromRgb(25, 25, 112);

    /// <summary>Mint cream (#F5FFFA).</summary>
    public static Color MintCream => FromRgb(245, 255, 250);

    /// <summary>Misty rose (#FFE4E1).</summary>
    public static Color MistyRose => FromRgb(255, 228, 225);

    /// <summary>Moccasin (#FFE4B5).</summary>
    public static Color Moccasin => FromRgb(255, 228, 181);

    /// <summary>Navajo white (#FFDEAD).</summary>
    public static Color NavajoWhite => FromRgb(255, 222, 173);

    /// <summary>Navy (#000080).</summary>
    public static Color Navy => FromRgb(0, 0, 128);

    /// <summary>Old lace (#FDF5E6).</summary>
    public static Color OldLace => FromRgb(253, 245, 230);

    /// <summary>Olive (#808000).</summary>
    public static Color Olive => FromRgb(128, 128, 0);

    /// <summary>Olive drab (#6B8E23).</summary>
    public static Color OliveDrab => FromRgb(107, 142, 35);

    /// <summary>Orange (#FFA500).</summary>
    public static Color Orange => FromRgb(255, 165, 0);

    /// <summary>Orange red (#FF4500).</summary>
    public static Color OrangeRed => FromRgb(255, 69, 0);

    /// <summary>Orchid (#DA70D6).</summary>
    public static Color Orchid => FromRgb(218, 112, 214);

    /// <summary>Pale goldenrod (#EEE8AA).</summary>
    public static Color PaleGoldenrod => FromRgb(238, 232, 170);

    /// <summary>Pale green (#98FB98).</summary>
    public static Color PaleGreen => FromRgb(152, 251, 152);

    /// <summary>Pale turquoise (#AFEEEE).</summary>
    public static Color PaleTurquoise => FromRgb(175, 238, 238);

    /// <summary>Pale violet red (#DB7093).</summary>
    public static Color PaleVioletRed => FromRgb(219, 112, 147);

    /// <summary>Papaya whip (#FFEFD5).</summary>
    public static Color PapayaWhip => FromRgb(255, 239, 213);

    /// <summary>Peach puff (#FFDAB9).</summary>
    public static Color PeachPuff => FromRgb(255, 218, 185);

    /// <summary>Peru (#CD853F).</summary>
    public static Color Peru => FromRgb(205, 133, 63);

    /// <summary>Pink (#FFC0CB).</summary>
    public static Color Pink => FromRgb(255, 192, 203);

    /// <summary>Plum (#DDA0DD).</summary>
    public static Color Plum => FromRgb(221, 160, 221);

    /// <summary>Powder blue (#B0E0E6).</summary>
    public static Color PowderBlue => FromRgb(176, 224, 230);

    /// <summary>Purple (#800080).</summary>
    public static Color Purple => FromRgb(128, 0, 128);

    /// <summary>Rebecca purple (#663399).</summary>
    public static Color RebeccaPurple => FromRgb(102, 51, 153);

    /// <summary>Red (#FF0000).</summary>
    public static Color Red => FromRgb(255, 0, 0);

    /// <summary>Rosy brown (#BC8F8F).</summary>
    public static Color RosyBrown => FromRgb(188, 143, 143);

    /// <summary>Royal blue (#4169E1).</summary>
    public static Color RoyalBlue => FromRgb(65, 105, 225);

    /// <summary>Saddle brown (#8B4513).</summary>
    public static Color SaddleBrown => FromRgb(139, 69, 19);

    /// <summary>Salmon (#FA8072).</summary>
    public static Color Salmon => FromRgb(250, 128, 114);

    /// <summary>Sandy brown (#F4A460).</summary>
    public static Color SandyBrown => FromRgb(244, 164, 96);

    /// <summary>Sea green (#2E8B57).</summary>
    public static Color SeaGreen => FromRgb(46, 139, 87);

    /// <summary>Sea shell (#FFF5EE).</summary>
    public static Color SeaShell => FromRgb(255, 245, 238);

    /// <summary>Sienna (#A0522D).</summary>
    public static Color Sienna => FromRgb(160, 82, 45);

    /// <summary>Silver (#C0C0C0).</summary>
    public static Color Silver => FromRgb(192, 192, 192);

    /// <summary>Sky blue (#87CEEB).</summary>
    public static Color SkyBlue => FromRgb(135, 206, 235);

    /// <summary>Slate blue (#6A5ACD).</summary>
    public static Color SlateBlue => FromRgb(106, 90, 205);

    /// <summary>Slate gray (#708090).</summary>
    public static Color SlateGray => FromRgb(112, 128, 144);

    /// <summary>Slate grey (#708090).</summary>
    public static Color SlateGrey => FromRgb(112, 128, 144);

    /// <summary>Snow (#FFFAFA).</summary>
    public static Color Snow => FromRgb(255, 250, 250);

    /// <summary>Spring green (#00FF7F).</summary>
    public static Color SpringGreen => FromRgb(0, 255, 127);

    /// <summary>Steel blue (#4682B4).</summary>
    public static Color SteelBlue => FromRgb(70, 130, 180);

    /// <summary>Tan (#D2B48C).</summary>
    public static Color Tan => FromRgb(210, 180, 140);

    /// <summary>Teal (#008080).</summary>
    public static Color Teal => FromRgb(0, 128, 128);

    /// <summary>Thistle (#D8BFD8).</summary>
    public static Color Thistle => FromRgb(216, 191, 216);

    /// <summary>Tomato (#FF6347).</summary>
    public static Color Tomato => FromRgb(255, 99, 71);

    /// <summary>Turquoise (#40E0D0).</summary>
    public static Color Turquoise => FromRgb(64, 224, 208);

    /// <summary>Violet (#EE82EE).</summary>
    public static Color Violet => FromRgb(238, 130, 238);

    /// <summary>Wheat (#F5DEB3).</summary>
    public static Color Wheat => FromRgb(245, 222, 179);

    /// <summary>White (#FFFFFF).</summary>
    public static Color White => FromRgb(255, 255, 255);

    /// <summary>White smoke (#F5F5F5).</summary>
    public static Color WhiteSmoke => FromRgb(245, 245, 245);

    /// <summary>Yellow (#FFFF00).</summary>
    public static Color Yellow => FromRgb(255, 255, 0);

    /// <summary>Yellow green (#9ACD32).</summary>
    public static Color YellowGreen => FromRgb(154, 205, 50);

    /// <summary>Transparent (#00000000).</summary>
    public static Color Transparent => Color.FromArgb(0, 0, 0, 0);

    /// <summary>
    /// Determines whether two colors are equal.
    /// </summary>
    public static bool operator ==(Color left, Color right) => left.Equals(right);

    /// <summary>
    /// Determines whether two colors are not equal.
    /// </summary>
    public static bool operator !=(Color left, Color right) => !left.Equals(right);

    /// <inheritdoc/>
    public bool Equals(Color other) => A == other.A && R == other.R && G == other.G && B == other.B;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is Color other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(A, R, G, B);

    /// <summary>
    /// Returns the color as a hex string <see cref="FromHex"/> reads back unchanged:
    /// <c>#RRGGBB</c> for an opaque color and <c>#RRGGBBAA</c> when the alpha channel is not
    /// fully opaque. Digits are upper-case and culture-invariant.
    /// </summary>
    public override string ToString()
        => A == 255
            ? string.Create(CultureInfo.InvariantCulture, $"#{R:X2}{G:X2}{B:X2}")
            : string.Create(CultureInfo.InvariantCulture, $"#{R:X2}{G:X2}{B:X2}{A:X2}");
}
