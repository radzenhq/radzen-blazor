namespace Radzen.Documents.Spreadsheet;

#nullable enable

// Per-character advance widths of Aptos Narrow 11 (the Normal font XlsxWriter emits) at 96dpi
// for ASCII 32..126, measured against Excel. Used to size auto fitted columns on export so
// their content renders without clipping in Excel regardless of the browser font metrics.
internal static class ExcelTextMetrics
{
    private static readonly double[] Regular =
    [
        2.54, 4.57, 5.08, 7.11, 7.62, 11.18, 8.63, 3.04, 4.57, 4.57, 7.11, 7.62,
        4.06, 4.57, 4.06, 4.57, 7.62, 7.62, 7.62, 7.62, 7.62, 7.62, 7.62, 7.62,
        7.62, 7.62, 4.06, 4.06, 7.62, 7.62, 7.62, 6.60, 11.69, 7.62, 8.12, 9.14,
        9.14, 7.62, 7.11, 9.65, 9.65, 3.55, 4.57, 7.62, 6.60, 10.67, 9.65, 9.65,
        7.62, 9.65, 8.12, 7.62, 6.60, 9.14, 7.62, 12.20, 7.62, 7.11, 7.11, 4.06,
        4.57, 4.06, 7.62, 6.09, 7.11, 7.11, 7.62, 7.11, 7.62, 7.11, 4.06, 6.60,
        7.62, 3.55, 3.55, 6.60, 3.55, 11.69, 7.62, 7.62, 7.62, 7.62, 4.57, 6.60,
        4.57, 7.62, 6.09, 9.65, 6.09, 6.09, 6.09, 4.06, 6.60, 4.06, 7.62
    ];

    private static readonly double[] Bold =
    [
        2.54, 4.57, 5.58, 7.11, 7.62, 11.18, 9.14, 3.04, 4.57, 4.57, 7.11, 7.62,
        4.06, 4.57, 4.06, 5.08, 7.62, 7.62, 7.62, 7.62, 7.62, 7.62, 7.62, 7.62,
        7.62, 7.62, 4.06, 4.06, 7.62, 7.62, 7.62, 6.60, 11.18, 8.12, 8.12, 9.14,
        9.14, 7.62, 7.11, 9.65, 9.65, 4.06, 5.08, 8.12, 6.60, 10.67, 9.65, 9.65,
        8.12, 9.65, 8.63, 8.12, 6.60, 9.14, 8.12, 12.20, 8.12, 7.62, 7.11, 4.57,
        5.08, 4.57, 7.62, 6.09, 7.11, 7.11, 7.62, 7.11, 7.62, 7.62, 4.57, 6.60,
        7.62, 3.55, 3.55, 7.11, 4.06, 11.69, 7.62, 7.62, 7.62, 7.62, 5.08, 6.60,
        4.57, 7.62, 6.60, 10.16, 6.60, 6.60, 6.09, 4.57, 7.11, 4.57, 7.62
    ];

    public static double EstimateWidth(string text, bool bold, double? fontSize)
    {
        var widths = bold ? Bold : Regular;
        var total = 0d;

        foreach (var ch in text)
        {
            total += ch is >= ' ' and <= '~' ? widths[ch - ' '] : widths['0' - ' '];
        }

        return total * (fontSize ?? 11) / 11;
    }

    // The single line that determines a cell's width: wrapped cells render hard line breaks so
    // the longest line governs; non-wrapped cells render with white-space: nowrap which collapses them.
    public static string DisplayLine(string text, bool wrapText)
    {
        if (!text.Contains('\n', System.StringComparison.Ordinal) && !text.Contains('\r', System.StringComparison.Ordinal))
        {
            return text;
        }

        text = text.Replace("\r", "", System.StringComparison.Ordinal);

        if (!wrapText)
        {
            return text.Replace('\n', ' ');
        }

        var longest = "";

        foreach (var line in text.Split('\n'))
        {
            if (line.Length > longest.Length)
            {
                longest = line;
            }
        }

        return longest;
    }
}
