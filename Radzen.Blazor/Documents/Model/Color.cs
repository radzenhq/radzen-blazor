using System;

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

    /// <summary>Black (#000000).</summary>
    public static Color Black => Color.FromRgb(0, 0, 0);

    /// <summary>White (#FFFFFF).</summary>
    public static Color White => Color.FromRgb(255, 255, 255);

    /// <summary>Red (#FF0000).</summary>
    public static Color Red => Color.FromRgb(255, 0, 0);

    /// <summary>Green (#008000).</summary>
    public static Color Green => Color.FromRgb(0, 128, 0);

    /// <summary>Blue (#0000FF).</summary>
    public static Color Blue => Color.FromRgb(0, 0, 255);

    /// <summary>Yellow (#FFFF00).</summary>
    public static Color Yellow => Color.FromRgb(255, 255, 0);

    /// <summary>Orange (#FFA500).</summary>
    public static Color Orange => Color.FromRgb(255, 165, 0);

    /// <summary>Gray (#808080).</summary>
    public static Color Gray => Color.FromRgb(128, 128, 128);

    /// <summary>Light gray (#D3D3D3).</summary>
    public static Color LightGray => Color.FromRgb(211, 211, 211);

    /// <summary>Dark gray (#A9A9A9).</summary>
    public static Color DarkGray => Color.FromRgb(169, 169, 169);

    /// <summary>Dark blue (#00008B).</summary>
    public static Color DarkBlue => Color.FromRgb(0, 0, 139);

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
}
