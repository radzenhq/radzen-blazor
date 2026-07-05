using System;

namespace Radzen.Documents.Spreadsheet;

#nullable enable

// XLSX column widths are in characters of the Normal font's maximum digit width (7px per character).
internal static class ColumnWidthConversion
{
    // Excel caps column width at 255 characters; 255 * 7 converts back to exactly 255.
    public const double MaxWidthInPixels = 255 * 7;

    public static double PixelsToChars(double pixels) => Math.Round(pixels / 7.0, 8);

    public static double CharsToPixels(double chars) => (256 * chars + Math.Truncate(128 / 7.0)) / 256 * 7;
}
