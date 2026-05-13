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
            LegendPosition = ChartLegendPosition.Right
        };

        // Auto-detect: first column is categories, remaining columns are value series
        var catStart = selection.Start;
        var catEnd = new CellRef(selection.End.Row, selection.Start.Column);
        var hasHeader = IsTextCell(sheet, selection.Start.Row, selection.Start.Column);
        var dataStartRow = hasHeader ? selection.Start.Row + 1 : selection.Start.Row;

        var catFormula = FormulaFormat.ToAbsoluteFormula(sheet,
            new RangeRef(new CellRef(dataStartRow, catStart.Column), new CellRef(catEnd.Row, catEnd.Column)));

        for (var col = selection.Start.Column + 1; col <= selection.End.Column; col++)
        {
            var series = new ChartSeries
            {
                Index = col - selection.Start.Column - 1,
                Categories = catFormula,
                Values = FormulaFormat.ToAbsoluteFormula(sheet,
                    new RangeRef(new CellRef(dataStartRow, col), new CellRef(selection.End.Row, col))),
            };

            if (hasHeader)
            {
                var headerCell = sheet.Cells[selection.Start.Row, col];
                series.Name = headerCell?.GetValueAsString();
            }

            chart.Series.Add(series);
        }

        // If only one column selected, use it as values with row indices as categories
        if (selection.Columns == 1)
        {
            var series = new ChartSeries
            {
                Index = 0,
                Values = FormulaFormat.ToAbsoluteFormula(sheet,
                    new RangeRef(new CellRef(dataStartRow, selection.Start.Column), new CellRef(selection.End.Row, selection.Start.Column))),
            };

            if (hasHeader)
            {
                var headerCell = sheet.Cells[selection.Start.Row, selection.Start.Column];
                series.Name = headerCell?.GetValueAsString();
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
}
