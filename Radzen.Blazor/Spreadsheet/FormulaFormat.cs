using System;
using Radzen.Documents.Spreadsheet;

namespace Radzen.Blazor.Spreadsheet;

#nullable enable

internal static class FormulaFormat
{
    public static string ToAbsoluteFormula(Worksheet sheet, RangeRef range)
    {
        ArgumentNullException.ThrowIfNull(sheet);

        var start = new CellRef(range.Start.Row, range.Start.Column)
        {
            IsRowAbsolute = true,
            IsColumnAbsolute = true,
        };
        var end = new CellRef(range.End.Row, range.End.Column)
        {
            IsRowAbsolute = true,
            IsColumnAbsolute = true,
        };

        return $"{QuoteSheetName(sheet.Name)}!{start}:{end}";
    }

    public static bool TryParseFormula(string? text, out RangeRef range)
    {
        range = default;

        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var trimmed = text.Trim();

        if (trimmed.StartsWith('='))
        {
            trimmed = trimmed[1..].Trim();
        }

        try
        {
            range = RangeRef.Parse(trimmed);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static string QuoteSheetName(string name)
    {
        if (name.Contains(' ', StringComparison.Ordinal) || name.Contains('\'', StringComparison.Ordinal))
        {
            return $"'{name.Replace("'", "''", StringComparison.Ordinal)}'";
        }

        return name;
    }
}
