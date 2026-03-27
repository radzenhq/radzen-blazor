using System;
using System.Collections.Generic;
using Radzen.Documents.Spreadsheet;

namespace Radzen.Blazor.Spreadsheet;

#nullable enable

/// <summary>
/// Creates chart objects from worksheet selections.
/// </summary>
public static class ChartFactory
{
    private const long DefaultChartWidthEmu = 4572000;  // ~480px
    private const long DefaultChartHeightEmu = 2762250; // ~290px

    /// <summary>
    /// Creates a chart from the current selection in the worksheet.
    /// </summary>
    public static SheetChart CreateFromSelection(Worksheet sheet, RangeRef selection, SpreadsheetChartType chartType)
    {
        ArgumentNullException.ThrowIfNull(sheet);

        var chart = new SheetChart
        {
            ChartType = chartType,
            AnchorMode = DrawingAnchorMode.OneCellAnchor,
            From = new CellAnchor
            {
                Row = selection.End.Row + 1,
                Column = selection.Start.Column,
            },
            Width = DefaultChartWidthEmu,
            Height = DefaultChartHeightEmu,
            ShowLegend = true,
            LegendPosition = LegendPosition.Right
        };

        // Auto-detect: first column is categories, remaining columns are value series
        var catStart = selection.Start;
        var catEnd = new CellRef(selection.End.Row, selection.Start.Column);
        var hasHeader = IsTextCell(sheet, selection.Start.Row, selection.Start.Column);
        var dataStartRow = hasHeader ? selection.Start.Row + 1 : selection.Start.Row;

        var sheetName = QuoteSheetName(sheet.Name);
        var catFormula = $"{sheetName}!${ColumnRef.ToString(catStart.Column)}${dataStartRow + 1}:${ColumnRef.ToString(catEnd.Column)}${catEnd.Row + 1}";

        for (var col = selection.Start.Column + 1; col <= selection.End.Column; col++)
        {
            var series = new ChartSeriesDefinition
            {
                Index = col - selection.Start.Column - 1,
                CategoryFormula = catFormula,
                ValueFormula = $"{sheetName}!${ColumnRef.ToString(col)}${dataStartRow + 1}:${ColumnRef.ToString(col)}${selection.End.Row + 1}"
            };

            if (hasHeader)
            {
                var headerCell = sheet.Cells[selection.Start.Row, col];
                series.Title = headerCell?.GetValueAsString();
            }

            chart.Series.Add(series);
        }

        // If only one column selected, use it as values with row indices as categories
        if (selection.Columns == 1)
        {
            var series = new ChartSeriesDefinition
            {
                Index = 0,
                ValueFormula = $"{sheetName}!${ColumnRef.ToString(selection.Start.Column)}${dataStartRow + 1}:${ColumnRef.ToString(selection.Start.Column)}${selection.End.Row + 1}"
            };

            if (hasHeader)
            {
                var headerCell = sheet.Cells[selection.Start.Row, selection.Start.Column];
                series.Title = headerCell?.GetValueAsString();
            }

            chart.Series.Add(series);
        }

        return chart;
    }

    private static bool IsTextCell(Worksheet sheet, int row, int col)
    {
        var cell = sheet.Cells[row, col];
        return cell?.Data.Type == CellDataType.String;
    }

    private static string QuoteSheetName(string name)
    {
        if (name.Contains(' ', StringComparison.Ordinal) || name.Contains('\'', StringComparison.Ordinal))
        {
            return $"'{name.Replace("'", "''", StringComparison.Ordinal)}'";
        }

        return name;
    }
}
