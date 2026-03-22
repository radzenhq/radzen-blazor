using System;
using System.Collections.Generic;

namespace Radzen.Documents.Spreadsheet;

#nullable enable

/// <summary>
/// Builds format codes from UI state for the Format Cells dialog.
/// </summary>
public static class FormatCodeBuilder
{
    /// <summary>
    /// Builds a format code string from the given parameters.
    /// </summary>
    public static string Build(NumberFormatCategory category, int decimalPlaces = 2,
        bool useThousandsSeparator = true, string currencySymbol = "$",
        string? selectedPreset = null)
    {
        var decimals = decimalPlaces > 0 ? "." + new string('0', decimalPlaces) : "";

        return category switch
        {
            NumberFormatCategory.General => "General",
            NumberFormatCategory.Number => (useThousandsSeparator ? "#,##0" : "0") + decimals,
            NumberFormatCategory.Currency => currencySymbol + (useThousandsSeparator ? "#,##0" : "0") + decimals,
            NumberFormatCategory.Accounting => BuildAccountingFormat(decimalPlaces, currencySymbol),
            NumberFormatCategory.Percentage => "0" + decimals + "%",
            NumberFormatCategory.Scientific => "0" + decimals + "E+00",
            NumberFormatCategory.Date => selectedPreset ?? "mm/dd/yyyy",
            NumberFormatCategory.Time => selectedPreset ?? "h:mm AM/PM",
            NumberFormatCategory.Text => "@",
            _ => "General"
        };
    }

    private static string BuildAccountingFormat(int decimalPlaces, string currencySymbol)
    {
        var decimals = decimalPlaces > 0 ? "." + new string('0', decimalPlaces) : "";
        var pos = $"_({currencySymbol}* #,##0{decimals}_)";
        var neg = $"_({currencySymbol}* (#,##0{decimals})";
        var zero = decimalPlaces > 0
            ? $"_({currencySymbol}* \"-\"{new string('?', decimalPlaces)}_)"
            : $"_({currencySymbol}* \"-\"_)";
        var text = "_(@_)";
        return $"{pos};{neg};{zero};{text}";
    }

    /// <summary>
    /// Gets the available currency symbols.
    /// </summary>
    public static IReadOnlyList<string> CurrencySymbols { get; } = ["$", "\u20AC", "\u00A3", "\u00A5"];
}
