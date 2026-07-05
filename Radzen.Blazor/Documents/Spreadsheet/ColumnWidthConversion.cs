using System;

namespace Radzen.Documents.Spreadsheet;

#nullable enable

// XLSX column widths are in characters of the Normal font's maximum digit width (MDW).
internal static class ColumnWidthConversion
{
    // Effective MDW of Aptos Narrow 11 at 96dpi, measured against Excel - the Normal font XlsxWriter emits.
    public const double DefaultMdw = 7.5;

    // Excel caps column width at 255 characters.
    public const double MaxWidthInPixels = 255 * DefaultMdw;

    public static double GetMdw(string? fontName, double? fontSize)
    {
        // XlsxReader normalizes the defaults to null: no name means Aptos Narrow, no size means 11.
        var baseMdw = fontName switch
        {
            null or "Aptos Narrow" => 7.5,
            _ => 7.0, // Calibri 11 and the pre-Aptos era default
        };

        return baseMdw * (fontSize ?? 11) / 11;
    }

    public static double PixelsToChars(double pixels, double mdw = DefaultMdw) => Math.Round(pixels / mdw, 8);

    public static double CharsToPixels(double chars, double mdw = DefaultMdw) => (256 * chars + Math.Truncate(128 / mdw)) / 256 * mdw;
}
