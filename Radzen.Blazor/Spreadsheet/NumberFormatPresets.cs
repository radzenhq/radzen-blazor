using System;
using System.Collections.Generic;

namespace Radzen.Documents.Spreadsheet;

#nullable enable

/// <summary>
/// Categories of number format codes.
/// </summary>
public enum NumberFormatCategory
{
    /// <summary>General format (no specific formatting).</summary>
    General,
    /// <summary>Number formats (e.g. 0, 0.00, #,##0).</summary>
    Number,
    /// <summary>Currency formats (e.g. $#,##0.00).</summary>
    Currency,
    /// <summary>Accounting formats (e.g. _($* #,##0.00_)).</summary>
    Accounting,
    /// <summary>Percentage formats (e.g. 0%, 0.00%).</summary>
    Percentage,
    /// <summary>Scientific formats (e.g. 0.00E+00).</summary>
    Scientific,
    /// <summary>Date formats (e.g. mm/dd/yyyy).</summary>
    Date,
    /// <summary>Time formats (e.g. h:mm AM/PM).</summary>
    Time,
    /// <summary>Text format.</summary>
    Text
}

/// <summary>
/// Provides mappings between Excel built-in numFmtId values and format codes.
/// </summary>
public static class NumberFormatPresets
{
    private static readonly Dictionary<int, string> IdToCode = new()
    {
        [0] = "General",
        [1] = "0",
        [2] = "0.00",
        [3] = "#,##0",
        [4] = "#,##0.00",
        [9] = "0%",
        [10] = "0.00%",
        [11] = "0.00E+00",
        [14] = "mm/dd/yyyy",
        [15] = "d-mmm-yy",
        [16] = "d-mmm",
        [17] = "mmm-yy",
        [18] = "h:mm AM/PM",
        [19] = "h:mm:ss AM/PM",
        [20] = "h:mm",
        [21] = "h:mm:ss",
        [22] = "m/d/yy h:mm",
        [37] = "#,##0;(#,##0)",
        [38] = "#,##0;[Red](#,##0)",
        [39] = "#,##0.00;(#,##0.00)",
        [40] = "#,##0.00;[Red](#,##0.00)",
        [44] = "_($* #,##0.00_);_($* (#,##0.00);_($* \"-\"??_);_(@_)",
        [49] = "@"
    };

    private static readonly Dictionary<string, int> CodeToId = BuildCodeToId();

    private static Dictionary<string, int> BuildCodeToId()
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var kvp in IdToCode)
        {
            map.TryAdd(kvp.Value, kvp.Key);
        }
        return map;
    }

    /// <summary>
    /// Gets the format code for a built-in numFmtId.
    /// Returns null for unknown IDs.
    /// </summary>
    public static string? GetFormatCode(int numFmtId)
    {
        return IdToCode.TryGetValue(numFmtId, out var code) ? code : null;
    }

    /// <summary>
    /// Gets the numFmtId for a known format code.
    /// Returns -1 for unknown format codes.
    /// </summary>
    public static int GetNumFmtId(string formatCode)
    {
        ArgumentNullException.ThrowIfNull(formatCode);
        return CodeToId.TryGetValue(formatCode, out var id) ? id : -1;
    }

    /// <summary>
    /// Determines the category of a format code.
    /// </summary>
    public static NumberFormatCategory GetCategory(string? formatCode)
    {
        if (string.IsNullOrEmpty(formatCode) ||
            string.Equals(formatCode, "General", StringComparison.OrdinalIgnoreCase))
        {
            return NumberFormatCategory.General;
        }

        if (formatCode == "@")
        {
            return NumberFormatCategory.Text;
        }

        if (formatCode.Contains('%', StringComparison.Ordinal))
        {
            return NumberFormatCategory.Percentage;
        }

        if (NumberFormat.IsDateFormat(formatCode))
        {
            var hasDate = formatCode.Contains('y', StringComparison.OrdinalIgnoreCase) ||
                          formatCode.Contains('d', StringComparison.OrdinalIgnoreCase);

            return hasDate ? NumberFormatCategory.Date : NumberFormatCategory.Time;
        }

        // Scientific: contains E+/E-/e+/e- pattern
        for (var i = 0; i < formatCode.Length - 1; i++)
        {
            if (formatCode[i] is 'E' or 'e' && formatCode[i + 1] is '+' or '-')
            {
                return NumberFormatCategory.Scientific;
            }
        }

        // Accounting: contains _( pattern (underscore followed by open paren)
        if (formatCode.Contains("_(", StringComparison.Ordinal))
        {
            return NumberFormatCategory.Accounting;
        }

        if (formatCode.Contains('$', StringComparison.Ordinal) || formatCode.Contains('\u20AC', StringComparison.Ordinal) ||
            formatCode.Contains('\u00A3', StringComparison.Ordinal) || formatCode.Contains('\u00A5', StringComparison.Ordinal))
        {
            return NumberFormatCategory.Currency;
        }

        return NumberFormatCategory.Number;
    }

    /// <summary>
    /// Gets the preset format codes for a given category.
    /// </summary>
    public static IReadOnlyList<(string Name, string FormatCode)> GetPresets(NumberFormatCategory category)
    {
        return category switch
        {
            NumberFormatCategory.General => [("General", "General")],
            NumberFormatCategory.Number =>
            [
                ("0", "0"),
                ("0.00", "0.00"),
                ("#,##0", "#,##0"),
                ("#,##0.00", "#,##0.00")
            ],
            NumberFormatCategory.Currency =>
            [
                ("$#,##0", "$#,##0"),
                ("$#,##0.00", "$#,##0.00")
            ],
            NumberFormatCategory.Accounting =>
            [
                ("$ 0.00", "_($* #,##0.00_);_($* (#,##0.00);_($* \"-\"??_);_(@_)"),
                ("$ 0", "_($* #,##0_);_($* (#,##0);_($* \"-\"_);_(@_)")
            ],
            NumberFormatCategory.Percentage =>
            [
                ("0%", "0%"),
                ("0.00%", "0.00%")
            ],
            NumberFormatCategory.Scientific =>
            [
                ("0.00E+00", "0.00E+00")
            ],
            NumberFormatCategory.Date =>
            [
                ("mm/dd/yyyy", "mm/dd/yyyy"),
                ("yyyy-mm-dd", "yyyy-mm-dd"),
                ("d-mmm-yy", "d-mmm-yy"),
                ("d-mmm", "d-mmm"),
                ("mmm-yy", "mmm-yy")
            ],
            NumberFormatCategory.Time =>
            [
                ("h:mm AM/PM", "h:mm AM/PM"),
                ("h:mm:ss", "h:mm:ss")
            ],
            NumberFormatCategory.Text => [("Text", "@")],
            _ => []
        };
    }
}
