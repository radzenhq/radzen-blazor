using System.Collections.Generic;

namespace Radzen.Documents.Spreadsheet;

#nullable enable

/// <summary>
/// Specifies the chart type for a spreadsheet chart.
/// </summary>
public enum SpreadsheetChartType
{
    /// <summary>Column chart (vertical bars).</summary>
    Column,
    /// <summary>Bar chart (horizontal bars).</summary>
    Bar,
    /// <summary>Stacked column chart.</summary>
    StackedColumn,
    /// <summary>Stacked bar chart.</summary>
    StackedBar,
    /// <summary>100% stacked column chart.</summary>
    FullStackedColumn,
    /// <summary>100% stacked bar chart.</summary>
    FullStackedBar,
    /// <summary>Line chart.</summary>
    Line,
    /// <summary>Area chart.</summary>
    Area,
    /// <summary>Stacked area chart.</summary>
    StackedArea,
    /// <summary>100% stacked area chart.</summary>
    FullStackedArea,
    /// <summary>Pie chart.</summary>
    Pie,
    /// <summary>Donut chart.</summary>
    Donut,
    /// <summary>Scatter (XY) chart.</summary>
    Scatter,
    /// <summary>Chart type not supported for rendering.</summary>
    Unsupported
}

/// <summary>
/// Represents a floating chart on a spreadsheet sheet.
/// </summary>
public class SheetChart
{
    /// <summary>
    /// Gets or sets the anchor mode.
    /// </summary>
    public DrawingAnchorMode AnchorMode { get; set; }

    /// <summary>
    /// Gets or sets the starting anchor position.
    /// </summary>
    public CellAnchor From { get; set; } = new();

    /// <summary>
    /// Gets or sets the ending anchor position (TwoCellAnchor only).
    /// </summary>
    public CellAnchor? To { get; set; }

    /// <summary>
    /// Gets or sets the chart width in pixels (OneCellAnchor only).
    /// </summary>
    public double Width { get; set; }

    /// <summary>
    /// Gets or sets the chart height in pixels (OneCellAnchor only).
    /// </summary>
    public double Height { get; set; }

    /// <summary>
    /// Gets or sets the chart type.
    /// </summary>
    public SpreadsheetChartType ChartType { get; set; }

    /// <summary>
    /// Gets or sets the chart title.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets the optional name.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets whether the legend is visible.
    /// </summary>
    public bool ShowLegend { get; set; }

    /// <summary>
    /// Gets or sets the legend position.
    /// </summary>
    public ChartLegendPosition LegendPosition { get; set; }

    /// <summary>
    /// Gets or sets the series definitions.
    /// </summary>
    public List<ChartSeriesDefinition> Series { get; set; } = [];

    /// <summary>
    /// Gets or sets the raw chart XML for lossless XLSX round-tripping.
    /// </summary>
    public string? RawChartXml { get; set; }
}

/// <summary>
/// Specifies the legend position for a spreadsheet chart.
/// </summary>
public enum ChartLegendPosition
{
    /// <summary>Legend on the right.</summary>
    Right,
    /// <summary>Legend on the left.</summary>
    Left,
    /// <summary>Legend at the top.</summary>
    Top,
    /// <summary>Legend at the bottom.</summary>
    Bottom
}

/// <summary>
/// Defines a data series within a spreadsheet chart.
/// </summary>
public class ChartSeriesDefinition
{
    /// <summary>
    /// Gets or sets the series title.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets the category range formula (e.g. "Sheet1!$A$2:$A$10").
    /// </summary>
    public string? CategoryFormula { get; set; }

    /// <summary>
    /// Gets or sets the value range formula (e.g. "Sheet1!$B$2:$B$10").
    /// </summary>
    public string? ValueFormula { get; set; }

    /// <summary>
    /// Gets or sets the cached category values from the XLSX file.
    /// </summary>
    public List<string> CategoryCache { get; set; } = [];

    /// <summary>
    /// Gets or sets the cached numeric values from the XLSX file.
    /// </summary>
    public List<double?> ValueCache { get; set; } = [];

    /// <summary>
    /// Gets or sets the series color.
    /// </summary>
    public string? Color { get; set; }

    /// <summary>
    /// Gets or sets the series index.
    /// </summary>
    public int Index { get; set; }
}
