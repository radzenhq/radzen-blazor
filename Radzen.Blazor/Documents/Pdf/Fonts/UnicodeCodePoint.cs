namespace Radzen.Documents.Pdf.Fonts;

internal static class UnicodeCodePoint
{
    public const int Replacement = 0xFFFD;

    public static int Sanitize(int value)
        => value is >= 0 and <= 0x10FFFF and (< 0xD800 or > 0xDFFF) ? value : Replacement;

    public static string ToString(int value) => char.ConvertFromUtf32(Sanitize(value));
}
