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
    public static Color AliceBlue => FromName(nameof(AliceBlue));

    /// <summary>Antique white (#FAEBD7).</summary>
    public static Color AntiqueWhite => FromName(nameof(AntiqueWhite));

    /// <summary>Aqua (#00FFFF).</summary>
    public static Color Aqua => FromName(nameof(Aqua));

    /// <summary>Aquamarine (#7FFFD4).</summary>
    public static Color Aquamarine => FromName(nameof(Aquamarine));

    /// <summary>Azure (#F0FFFF).</summary>
    public static Color Azure => FromName(nameof(Azure));

    /// <summary>Beige (#F5F5DC).</summary>
    public static Color Beige => FromName(nameof(Beige));

    /// <summary>Bisque (#FFE4C4).</summary>
    public static Color Bisque => FromName(nameof(Bisque));

    /// <summary>Black (#000000).</summary>
    public static Color Black => FromName(nameof(Black));

    /// <summary>Blanched almond (#FFEBCD).</summary>
    public static Color BlanchedAlmond => FromName(nameof(BlanchedAlmond));

    /// <summary>Blue (#0000FF).</summary>
    public static Color Blue => FromName(nameof(Blue));

    /// <summary>Blue violet (#8A2BE2).</summary>
    public static Color BlueViolet => FromName(nameof(BlueViolet));

    /// <summary>Brown (#A52A2A).</summary>
    public static Color Brown => FromName(nameof(Brown));

    /// <summary>Burly wood (#DEB887).</summary>
    public static Color BurlyWood => FromName(nameof(BurlyWood));

    /// <summary>Cadet blue (#5F9EA0).</summary>
    public static Color CadetBlue => FromName(nameof(CadetBlue));

    /// <summary>Chartreuse (#7FFF00).</summary>
    public static Color Chartreuse => FromName(nameof(Chartreuse));

    /// <summary>Chocolate (#D2691E).</summary>
    public static Color Chocolate => FromName(nameof(Chocolate));

    /// <summary>Coral (#FF7F50).</summary>
    public static Color Coral => FromName(nameof(Coral));

    /// <summary>Cornflower blue (#6495ED).</summary>
    public static Color CornflowerBlue => FromName(nameof(CornflowerBlue));

    /// <summary>Cornsilk (#FFF8DC).</summary>
    public static Color Cornsilk => FromName(nameof(Cornsilk));

    /// <summary>Crimson (#DC143C).</summary>
    public static Color Crimson => FromName(nameof(Crimson));

    /// <summary>Cyan (#00FFFF).</summary>
    public static Color Cyan => FromName(nameof(Cyan));

    /// <summary>Dark blue (#00008B).</summary>
    public static Color DarkBlue => FromName(nameof(DarkBlue));

    /// <summary>Dark cyan (#008B8B).</summary>
    public static Color DarkCyan => FromName(nameof(DarkCyan));

    /// <summary>Dark goldenrod (#B8860B).</summary>
    public static Color DarkGoldenrod => FromName(nameof(DarkGoldenrod));

    /// <summary>Dark gray (#A9A9A9).</summary>
    public static Color DarkGray => FromName(nameof(DarkGray));

    /// <summary>Dark green (#006400).</summary>
    public static Color DarkGreen => FromName(nameof(DarkGreen));

    /// <summary>Dark grey (#A9A9A9).</summary>
    public static Color DarkGrey => FromName(nameof(DarkGrey));

    /// <summary>Dark khaki (#BDB76B).</summary>
    public static Color DarkKhaki => FromName(nameof(DarkKhaki));

    /// <summary>Dark magenta (#8B008B).</summary>
    public static Color DarkMagenta => FromName(nameof(DarkMagenta));

    /// <summary>Dark olive green (#556B2F).</summary>
    public static Color DarkOliveGreen => FromName(nameof(DarkOliveGreen));

    /// <summary>Dark orange (#FF8C00).</summary>
    public static Color DarkOrange => FromName(nameof(DarkOrange));

    /// <summary>Dark orchid (#9932CC).</summary>
    public static Color DarkOrchid => FromName(nameof(DarkOrchid));

    /// <summary>Dark red (#8B0000).</summary>
    public static Color DarkRed => FromName(nameof(DarkRed));

    /// <summary>Dark salmon (#E9967A).</summary>
    public static Color DarkSalmon => FromName(nameof(DarkSalmon));

    /// <summary>Dark sea green (#8FBC8F).</summary>
    public static Color DarkSeaGreen => FromName(nameof(DarkSeaGreen));

    /// <summary>Dark slate blue (#483D8B).</summary>
    public static Color DarkSlateBlue => FromName(nameof(DarkSlateBlue));

    /// <summary>Dark slate gray (#2F4F4F).</summary>
    public static Color DarkSlateGray => FromName(nameof(DarkSlateGray));

    /// <summary>Dark slate grey (#2F4F4F).</summary>
    public static Color DarkSlateGrey => FromName(nameof(DarkSlateGrey));

    /// <summary>Dark turquoise (#00CED1).</summary>
    public static Color DarkTurquoise => FromName(nameof(DarkTurquoise));

    /// <summary>Dark violet (#9400D3).</summary>
    public static Color DarkViolet => FromName(nameof(DarkViolet));

    /// <summary>Deep pink (#FF1493).</summary>
    public static Color DeepPink => FromName(nameof(DeepPink));

    /// <summary>Deep sky blue (#00BFFF).</summary>
    public static Color DeepSkyBlue => FromName(nameof(DeepSkyBlue));

    /// <summary>Dim gray (#696969).</summary>
    public static Color DimGray => FromName(nameof(DimGray));

    /// <summary>Dim grey (#696969).</summary>
    public static Color DimGrey => FromName(nameof(DimGrey));

    /// <summary>Dodger blue (#1E90FF).</summary>
    public static Color DodgerBlue => FromName(nameof(DodgerBlue));

    /// <summary>Fire brick (#B22222).</summary>
    public static Color FireBrick => FromName(nameof(FireBrick));

    /// <summary>Floral white (#FFFAF0).</summary>
    public static Color FloralWhite => FromName(nameof(FloralWhite));

    /// <summary>Forest green (#228B22).</summary>
    public static Color ForestGreen => FromName(nameof(ForestGreen));

    /// <summary>Fuchsia (#FF00FF).</summary>
    public static Color Fuchsia => FromName(nameof(Fuchsia));

    /// <summary>Gainsboro (#DCDCDC).</summary>
    public static Color Gainsboro => FromName(nameof(Gainsboro));

    /// <summary>Ghost white (#F8F8FF).</summary>
    public static Color GhostWhite => FromName(nameof(GhostWhite));

    /// <summary>Gold (#FFD700).</summary>
    public static Color Gold => FromName(nameof(Gold));

    /// <summary>Goldenrod (#DAA520).</summary>
    public static Color Goldenrod => FromName(nameof(Goldenrod));

    /// <summary>Gray (#808080).</summary>
    public static Color Gray => FromName(nameof(Gray));

    /// <summary>Green (#008000).</summary>
    public static Color Green => FromName(nameof(Green));

    /// <summary>Green yellow (#ADFF2F).</summary>
    public static Color GreenYellow => FromName(nameof(GreenYellow));

    /// <summary>Grey (#808080).</summary>
    public static Color Grey => FromName(nameof(Grey));

    /// <summary>Honeydew (#F0FFF0).</summary>
    public static Color Honeydew => FromName(nameof(Honeydew));

    /// <summary>Hot pink (#FF69B4).</summary>
    public static Color HotPink => FromName(nameof(HotPink));

    /// <summary>Indian red (#CD5C5C).</summary>
    public static Color IndianRed => FromName(nameof(IndianRed));

    /// <summary>Indigo (#4B0082).</summary>
    public static Color Indigo => FromName(nameof(Indigo));

    /// <summary>Ivory (#FFFFF0).</summary>
    public static Color Ivory => FromName(nameof(Ivory));

    /// <summary>Khaki (#F0E68C).</summary>
    public static Color Khaki => FromName(nameof(Khaki));

    /// <summary>Lavender (#E6E6FA).</summary>
    public static Color Lavender => FromName(nameof(Lavender));

    /// <summary>Lavender blush (#FFF0F5).</summary>
    public static Color LavenderBlush => FromName(nameof(LavenderBlush));

    /// <summary>Lawn green (#7CFC00).</summary>
    public static Color LawnGreen => FromName(nameof(LawnGreen));

    /// <summary>Lemon chiffon (#FFFACD).</summary>
    public static Color LemonChiffon => FromName(nameof(LemonChiffon));

    /// <summary>Light blue (#ADD8E6).</summary>
    public static Color LightBlue => FromName(nameof(LightBlue));

    /// <summary>Light coral (#F08080).</summary>
    public static Color LightCoral => FromName(nameof(LightCoral));

    /// <summary>Light cyan (#E0FFFF).</summary>
    public static Color LightCyan => FromName(nameof(LightCyan));

    /// <summary>Light goldenrod yellow (#FAFAD2).</summary>
    public static Color LightGoldenrodYellow => FromName(nameof(LightGoldenrodYellow));

    /// <summary>Light gray (#D3D3D3).</summary>
    public static Color LightGray => FromName(nameof(LightGray));

    /// <summary>Light green (#90EE90).</summary>
    public static Color LightGreen => FromName(nameof(LightGreen));

    /// <summary>Light grey (#D3D3D3).</summary>
    public static Color LightGrey => FromName(nameof(LightGrey));

    /// <summary>Light pink (#FFB6C1).</summary>
    public static Color LightPink => FromName(nameof(LightPink));

    /// <summary>Light salmon (#FFA07A).</summary>
    public static Color LightSalmon => FromName(nameof(LightSalmon));

    /// <summary>Light sea green (#20B2AA).</summary>
    public static Color LightSeaGreen => FromName(nameof(LightSeaGreen));

    /// <summary>Light sky blue (#87CEFA).</summary>
    public static Color LightSkyBlue => FromName(nameof(LightSkyBlue));

    /// <summary>Light slate gray (#778899).</summary>
    public static Color LightSlateGray => FromName(nameof(LightSlateGray));

    /// <summary>Light slate grey (#778899).</summary>
    public static Color LightSlateGrey => FromName(nameof(LightSlateGrey));

    /// <summary>Light steel blue (#B0C4DE).</summary>
    public static Color LightSteelBlue => FromName(nameof(LightSteelBlue));

    /// <summary>Light yellow (#FFFFE0).</summary>
    public static Color LightYellow => FromName(nameof(LightYellow));

    /// <summary>Lime (#00FF00).</summary>
    public static Color Lime => FromName(nameof(Lime));

    /// <summary>Lime green (#32CD32).</summary>
    public static Color LimeGreen => FromName(nameof(LimeGreen));

    /// <summary>Linen (#FAF0E6).</summary>
    public static Color Linen => FromName(nameof(Linen));

    /// <summary>Magenta (#FF00FF).</summary>
    public static Color Magenta => FromName(nameof(Magenta));

    /// <summary>Maroon (#800000).</summary>
    public static Color Maroon => FromName(nameof(Maroon));

    /// <summary>Medium aquamarine (#66CDAA).</summary>
    public static Color MediumAquamarine => FromName(nameof(MediumAquamarine));

    /// <summary>Medium blue (#0000CD).</summary>
    public static Color MediumBlue => FromName(nameof(MediumBlue));

    /// <summary>Medium orchid (#BA55D3).</summary>
    public static Color MediumOrchid => FromName(nameof(MediumOrchid));

    /// <summary>Medium purple (#9370DB).</summary>
    public static Color MediumPurple => FromName(nameof(MediumPurple));

    /// <summary>Medium sea green (#3CB371).</summary>
    public static Color MediumSeaGreen => FromName(nameof(MediumSeaGreen));

    /// <summary>Medium slate blue (#7B68EE).</summary>
    public static Color MediumSlateBlue => FromName(nameof(MediumSlateBlue));

    /// <summary>Medium spring green (#00FA9A).</summary>
    public static Color MediumSpringGreen => FromName(nameof(MediumSpringGreen));

    /// <summary>Medium turquoise (#48D1CC).</summary>
    public static Color MediumTurquoise => FromName(nameof(MediumTurquoise));

    /// <summary>Medium violet red (#C71585).</summary>
    public static Color MediumVioletRed => FromName(nameof(MediumVioletRed));

    /// <summary>Midnight blue (#191970).</summary>
    public static Color MidnightBlue => FromName(nameof(MidnightBlue));

    /// <summary>Mint cream (#F5FFFA).</summary>
    public static Color MintCream => FromName(nameof(MintCream));

    /// <summary>Misty rose (#FFE4E1).</summary>
    public static Color MistyRose => FromName(nameof(MistyRose));

    /// <summary>Moccasin (#FFE4B5).</summary>
    public static Color Moccasin => FromName(nameof(Moccasin));

    /// <summary>Navajo white (#FFDEAD).</summary>
    public static Color NavajoWhite => FromName(nameof(NavajoWhite));

    /// <summary>Navy (#000080).</summary>
    public static Color Navy => FromName(nameof(Navy));

    /// <summary>Old lace (#FDF5E6).</summary>
    public static Color OldLace => FromName(nameof(OldLace));

    /// <summary>Olive (#808000).</summary>
    public static Color Olive => FromName(nameof(Olive));

    /// <summary>Olive drab (#6B8E23).</summary>
    public static Color OliveDrab => FromName(nameof(OliveDrab));

    /// <summary>Orange (#FFA500).</summary>
    public static Color Orange => FromName(nameof(Orange));

    /// <summary>Orange red (#FF4500).</summary>
    public static Color OrangeRed => FromName(nameof(OrangeRed));

    /// <summary>Orchid (#DA70D6).</summary>
    public static Color Orchid => FromName(nameof(Orchid));

    /// <summary>Pale goldenrod (#EEE8AA).</summary>
    public static Color PaleGoldenrod => FromName(nameof(PaleGoldenrod));

    /// <summary>Pale green (#98FB98).</summary>
    public static Color PaleGreen => FromName(nameof(PaleGreen));

    /// <summary>Pale turquoise (#AFEEEE).</summary>
    public static Color PaleTurquoise => FromName(nameof(PaleTurquoise));

    /// <summary>Pale violet red (#DB7093).</summary>
    public static Color PaleVioletRed => FromName(nameof(PaleVioletRed));

    /// <summary>Papaya whip (#FFEFD5).</summary>
    public static Color PapayaWhip => FromName(nameof(PapayaWhip));

    /// <summary>Peach puff (#FFDAB9).</summary>
    public static Color PeachPuff => FromName(nameof(PeachPuff));

    /// <summary>Peru (#CD853F).</summary>
    public static Color Peru => FromName(nameof(Peru));

    /// <summary>Pink (#FFC0CB).</summary>
    public static Color Pink => FromName(nameof(Pink));

    /// <summary>Plum (#DDA0DD).</summary>
    public static Color Plum => FromName(nameof(Plum));

    /// <summary>Powder blue (#B0E0E6).</summary>
    public static Color PowderBlue => FromName(nameof(PowderBlue));

    /// <summary>Purple (#800080).</summary>
    public static Color Purple => FromName(nameof(Purple));

    /// <summary>Rebecca purple (#663399).</summary>
    public static Color RebeccaPurple => FromName(nameof(RebeccaPurple));

    /// <summary>Red (#FF0000).</summary>
    public static Color Red => FromName(nameof(Red));

    /// <summary>Rosy brown (#BC8F8F).</summary>
    public static Color RosyBrown => FromName(nameof(RosyBrown));

    /// <summary>Royal blue (#4169E1).</summary>
    public static Color RoyalBlue => FromName(nameof(RoyalBlue));

    /// <summary>Saddle brown (#8B4513).</summary>
    public static Color SaddleBrown => FromName(nameof(SaddleBrown));

    /// <summary>Salmon (#FA8072).</summary>
    public static Color Salmon => FromName(nameof(Salmon));

    /// <summary>Sandy brown (#F4A460).</summary>
    public static Color SandyBrown => FromName(nameof(SandyBrown));

    /// <summary>Sea green (#2E8B57).</summary>
    public static Color SeaGreen => FromName(nameof(SeaGreen));

    /// <summary>Sea shell (#FFF5EE).</summary>
    public static Color SeaShell => FromName(nameof(SeaShell));

    /// <summary>Sienna (#A0522D).</summary>
    public static Color Sienna => FromName(nameof(Sienna));

    /// <summary>Silver (#C0C0C0).</summary>
    public static Color Silver => FromName(nameof(Silver));

    /// <summary>Sky blue (#87CEEB).</summary>
    public static Color SkyBlue => FromName(nameof(SkyBlue));

    /// <summary>Slate blue (#6A5ACD).</summary>
    public static Color SlateBlue => FromName(nameof(SlateBlue));

    /// <summary>Slate gray (#708090).</summary>
    public static Color SlateGray => FromName(nameof(SlateGray));

    /// <summary>Slate grey (#708090).</summary>
    public static Color SlateGrey => FromName(nameof(SlateGrey));

    /// <summary>Snow (#FFFAFA).</summary>
    public static Color Snow => FromName(nameof(Snow));

    /// <summary>Spring green (#00FF7F).</summary>
    public static Color SpringGreen => FromName(nameof(SpringGreen));

    /// <summary>Steel blue (#4682B4).</summary>
    public static Color SteelBlue => FromName(nameof(SteelBlue));

    /// <summary>Tan (#D2B48C).</summary>
    public static Color Tan => FromName(nameof(Tan));

    /// <summary>Teal (#008080).</summary>
    public static Color Teal => FromName(nameof(Teal));

    /// <summary>Thistle (#D8BFD8).</summary>
    public static Color Thistle => FromName(nameof(Thistle));

    /// <summary>Tomato (#FF6347).</summary>
    public static Color Tomato => FromName(nameof(Tomato));

    /// <summary>Turquoise (#40E0D0).</summary>
    public static Color Turquoise => FromName(nameof(Turquoise));

    /// <summary>Violet (#EE82EE).</summary>
    public static Color Violet => FromName(nameof(Violet));

    /// <summary>Wheat (#F5DEB3).</summary>
    public static Color Wheat => FromName(nameof(Wheat));

    /// <summary>White (#FFFFFF).</summary>
    public static Color White => FromName(nameof(White));

    /// <summary>White smoke (#F5F5F5).</summary>
    public static Color WhiteSmoke => FromName(nameof(WhiteSmoke));

    /// <summary>Yellow (#FFFF00).</summary>
    public static Color Yellow => FromName(nameof(Yellow));

    /// <summary>Yellow green (#9ACD32).</summary>
    public static Color YellowGreen => FromName(nameof(YellowGreen));

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
