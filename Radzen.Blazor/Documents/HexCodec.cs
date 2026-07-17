using System;

namespace Radzen.Documents;

/// <summary>
/// Which alphabet <see cref="HexCodec"/> emits.
/// </summary>
internal enum HexCase
{
    /// <summary>Emit <c>0123456789ABCDEF</c>.</summary>
    Upper,

    /// <summary>Emit <c>0123456789abcdef</c>.</summary>
    Lower,
}

/// <summary>
/// Byte to base-16 encoding.
/// </summary>
// Case is a caller concern, not a formatting preference: a PAdES /VRI key is an uppercase
// digest string that readers match literally, and every other caller's case is byte-visible
// in the file it writes.
internal static class HexCodec
{
    const string UpperDigits = "0123456789ABCDEF";
    const string LowerDigits = "0123456789abcdef";

    static string Digits(HexCase hexCase) => hexCase == HexCase.Upper ? UpperDigits : LowerDigits;

    /// <summary>
    /// Writes two hex bytes per input byte into <paramref name="destination"/>.
    /// </summary>
    public static void Encode(ReadOnlySpan<byte> data, Span<byte> destination, HexCase hexCase)
    {
        if (destination.Length < data.Length * 2)
        {
            throw new ArgumentException("Destination is too small for the encoded output.", nameof(destination));
        }

        var digits = Digits(hexCase);

        for (var i = 0; i < data.Length; i++)
        {
            destination[i * 2] = (byte)digits[data[i] >> 4];
            destination[i * 2 + 1] = (byte)digits[data[i] & 0x0F];
        }
    }

    /// <summary>
    /// Returns two hex characters per input byte.
    /// </summary>
    public static string EncodeToString(ReadOnlySpan<byte> data, HexCase hexCase)
    {
        var digits = Digits(hexCase);
        var chars = new char[data.Length * 2];

        for (var i = 0; i < data.Length; i++)
        {
            chars[i * 2] = digits[data[i] >> 4];
            chars[i * 2 + 1] = digits[data[i] & 0x0F];
        }

        return new string(chars);
    }
}
