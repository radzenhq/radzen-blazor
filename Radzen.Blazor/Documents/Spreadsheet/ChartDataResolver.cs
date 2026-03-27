using System;
using System.Collections.Generic;
using System.Globalization;

namespace Radzen.Documents.Spreadsheet;

#nullable enable

/// <summary>
/// Resolves chart series data from cell range formulas.
/// </summary>
public static class ChartDataResolver
{
    /// <summary>
    /// Parses a range formula like "Sheet1!$A$2:$B$10" or "$A$2:$B$10" into a sheet name and range.
    /// </summary>
    public static (string? sheetName, RangeRef range) ParseRangeFormula(string formula)
    {
        ArgumentNullException.ThrowIfNull(formula);

        string? sheetName = null;

        var bang = formula.IndexOf('!', StringComparison.Ordinal);

        if (bang >= 0)
        {
            sheetName = formula[..bang];

            // Remove surrounding quotes from sheet name if present
            if (sheetName.Length >= 2 && sheetName[0] == '\'' && sheetName[^1] == '\'')
            {
                sheetName = sheetName[1..^1].Replace("''", "'", StringComparison.Ordinal);
            }

            formula = formula[(bang + 1)..];
        }

        // Handle single cell reference (no colon)
        if (formula.IndexOf(':', StringComparison.Ordinal) < 0)
        {
            var cell = CellRef.Parse(formula);
            return (sheetName, new RangeRef(cell, cell));
        }

        var parts = formula.Split(':');
        var start = CellRef.Parse(parts[0]);
        var end = CellRef.Parse(parts[1]);

        return (sheetName, new RangeRef(start, end));
    }

    /// <summary>
    /// Resolves chart data points for a series definition from the workbook.
    /// Falls back to cached values when live resolution fails.
    /// </summary>
    public static List<ChartDataPoint> ResolveSeriesData(ChartSeriesDefinition series, Workbook workbook, Worksheet currentSheet)
    {
        ArgumentNullException.ThrowIfNull(series);

        var categories = ResolveCategories(series, workbook, currentSheet);
        var values = ResolveValues(series, workbook, currentSheet);

        var count = Math.Max(categories.Count, values.Count);
        var result = new List<ChartDataPoint>(count);

        for (var i = 0; i < count; i++)
        {
            result.Add(new ChartDataPoint
            {
                Category = i < categories.Count ? categories[i] : (i + 1).ToString(CultureInfo.InvariantCulture),
                Value = i < values.Count ? values[i] : 0
            });
        }

        return result;
    }

    private static List<string> ResolveCategories(ChartSeriesDefinition series, Workbook workbook, Worksheet currentSheet)
    {
        if (!string.IsNullOrEmpty(series.CategoryFormula))
        {
            try
            {
                var (sheetName, range) = ParseRangeFormula(series.CategoryFormula);
                var sheet = ResolveSheet(sheetName, workbook, currentSheet);

                if (sheet != null)
                {
                    var categories = new List<string>();

                    for (var row = range.Start.Row; row <= range.End.Row; row++)
                    {
                        for (var col = range.Start.Column; col <= range.End.Column; col++)
                        {
                            var cell = sheet.Cells[row, col];
                            categories.Add(cell?.GetValueAsString() ?? "");
                        }
                    }

                    if (categories.Count > 0)
                    {
                        return categories;
                    }
                }
            }
            catch
            {
                // Fall through to cache
            }
        }

        // Fall back to cached values
        return series.CategoryCache.Count > 0
            ? new List<string>(series.CategoryCache)
            : [];
    }

    private static List<double> ResolveValues(ChartSeriesDefinition series, Workbook workbook, Worksheet currentSheet)
    {
        if (!string.IsNullOrEmpty(series.ValueFormula))
        {
            try
            {
                var (sheetName, range) = ParseRangeFormula(series.ValueFormula);
                var sheet = ResolveSheet(sheetName, workbook, currentSheet);

                if (sheet != null)
                {
                    var values = new List<double>();

                    for (var row = range.Start.Row; row <= range.End.Row; row++)
                    {
                        for (var col = range.Start.Column; col <= range.End.Column; col++)
                        {
                            var cell = sheet.Cells[row, col];

                            if (cell?.Value is double d)
                            {
                                values.Add(d);
                            }
                            else if (cell?.Data.Type == CellDataType.Number && cell.Value != null)
                            {
                                values.Add(Convert.ToDouble(cell.Value, CultureInfo.InvariantCulture));
                            }
                            else
                            {
                                values.Add(0);
                            }
                        }
                    }

                    if (values.Count > 0)
                    {
                        return values;
                    }
                }
            }
            catch
            {
                // Fall through to cache
            }
        }

        // Fall back to cached values
        if (series.ValueCache.Count > 0)
        {
            var values = new List<double>(series.ValueCache.Count);

            foreach (var v in series.ValueCache)
            {
                values.Add(v ?? 0);
            }

            return values;
        }

        return [];
    }

    private static Worksheet? ResolveSheet(string? sheetName, Workbook workbook, Worksheet currentSheet)
    {
        if (string.IsNullOrEmpty(sheetName))
        {
            return currentSheet;
        }

        foreach (var sheet in workbook.Sheets)
        {
            if (string.Equals(sheet.Name, sheetName, StringComparison.OrdinalIgnoreCase))
            {
                return sheet;
            }
        }

        return null;
    }
}
