using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Radzen.Blazor.Rendering;
using System.Threading.Tasks;

#nullable enable
using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet;

/// <summary>
/// Renders floating charts on a spreadsheet sheet.
/// </summary>
public partial class ChartOverlay : ComponentBase, IDisposable
{
    private const double EmuPerPixel = 9525.0;

    private readonly Dictionary<SheetChart, List<(ChartSeriesDefinition series, List<ChartDataPoint> data)>> seriesCache = [];

    /// <summary>
    /// Gets or sets the sheet.
    /// </summary>
    [Parameter]
    public Worksheet Worksheet { get; set; } = default!;

    /// <summary>
    /// Gets or sets the virtual grid context.
    /// </summary>
    [Parameter]
    public IVirtualGridContext Context { get; set; } = default!;

    /// <inheritdoc/>
    public override async Task SetParametersAsync(ParameterView parameters)
    {
        var previousWorksheet = Worksheet;

        if (Worksheet is not null)
        {
            Worksheet.ChartsChanged -= OnChartsChanged;
            Worksheet.SelectedChartChanged -= OnChanged;
        }

        await base.SetParametersAsync(parameters);

        if (Worksheet is not null)
        {
            Worksheet.ChartsChanged += OnChartsChanged;
            Worksheet.SelectedChartChanged += OnChanged;
        }

        if (previousWorksheet != Worksheet)
        {
            seriesCache.Clear();
        }
    }

    private void OnChartsChanged()
    {
        seriesCache.Clear();
        StateHasChanged();
    }

    private void OnChanged()
    {
        StateHasChanged();
    }

    void IDisposable.Dispose()
    {
        seriesCache.Clear();

        if (Worksheet is not null)
        {
            Worksheet.ChartsChanged -= OnChartsChanged;
            Worksheet.SelectedChartChanged -= OnChanged;
        }
    }

    private RangeRef GetChartRange(SheetChart chart)
    {
        if (chart.AnchorMode == DrawingAnchorMode.TwoCellAnchor && chart.To is not null)
        {
            return new RangeRef(
                new CellRef(chart.From.Row, chart.From.Column),
                new CellRef(chart.To.Row, chart.To.Column));
        }

        var widthPx = chart.Width / EmuPerPixel;
        var heightPx = chart.Height / EmuPerPixel;

        var endCol = chart.From.Column;
        var remaining = widthPx - (Context.GetRectangle(chart.From.Row, chart.From.Column).Width - chart.From.ColumnOffset / EmuPerPixel);
        while (remaining > 0 && endCol < Worksheet.ColumnCount - 1)
        {
            endCol++;
            remaining -= Context.GetRectangle(chart.From.Row, endCol).Width;
        }

        var endRow = chart.From.Row;
        remaining = heightPx - (Context.GetRectangle(chart.From.Row, chart.From.Column).Height - chart.From.RowOffset / EmuPerPixel);
        while (remaining > 0 && endRow < Worksheet.RowCount - 1)
        {
            endRow++;
            remaining -= Context.GetRectangle(endRow, chart.From.Column).Height;
        }

        return new RangeRef(
            new CellRef(chart.From.Row, chart.From.Column),
            new CellRef(endRow, endCol));
    }

    private string GetZoneStyle(SheetChart chart, RangeRef chartRange, RangeInfo zone)
    {
        var rect = Context.GetRectangle(zone.Range.Start.Row, zone.Range.Start.Column, zone.Range.End.Row, zone.Range.End.Column);
        return $"transform: translate({rect.Left.ToPx()}, {rect.Top.ToPx()}); width: {rect.Width.ToPx()}; height: {rect.Height.ToPx()};";
    }

    private (double width, double height) GetChartDimensions(SheetChart chart, RangeRef chartRange)
    {
        if (chart.AnchorMode == DrawingAnchorMode.TwoCellAnchor && chart.To is not null)
        {
            var fullRect = Context.GetRectangle(chartRange.Start.Row, chartRange.Start.Column, chartRange.End.Row, chartRange.End.Column);
            return (
                fullRect.Width + chart.From.ColumnOffset / EmuPerPixel + chart.To.ColumnOffset / EmuPerPixel,
                fullRect.Height + chart.From.RowOffset / EmuPerPixel + chart.To.RowOffset / EmuPerPixel);
        }

        return (chart.Width / EmuPerPixel, chart.Height / EmuPerPixel);
    }

    private (double x, double y) GetZoneOffset(SheetChart chart, RangeInfo zone)
    {
        double offsetX = chart.From.ColumnOffset / EmuPerPixel;
        double offsetY = chart.From.RowOffset / EmuPerPixel;

        for (var col = chart.From.Column; col < zone.Range.Start.Column; col++)
        {
            offsetX -= Worksheet.Columns[col];
        }

        for (var row = chart.From.Row; row < zone.Range.Start.Row; row++)
        {
            offsetY -= Worksheet.Rows[row];
        }

        return (offsetX, offsetY);
    }

    private string GetChartContainerStyle(SheetChart chart, RangeRef chartRange, RangeInfo zone)
    {
        var (chartWidth, chartHeight) = GetChartDimensions(chart, chartRange);
        var (offsetX, offsetY) = GetZoneOffset(chart, zone);

        return $"position: absolute; left: {offsetX.ToPx()}; top: {offsetY.ToPx()}; width: {chartWidth.ToPx()}; height: {chartHeight.ToPx()};";
    }

    private static string GetClass(bool frozenRow, bool frozenColumn)
    {
        return ClassList.Create("rz-spreadsheet-chart")
            .Add("rz-spreadsheet-frozen-column", frozenColumn)
            .Add("rz-spreadsheet-frozen-row", frozenRow)
            .ToString();
    }

    private static string GetSelectionClass(RangeInfo zone)
    {
        return ClassList.Create("rz-spreadsheet-selection-range")
            .Add("rz-spreadsheet-selection-range-top", zone.Top)
            .Add("rz-spreadsheet-selection-range-left", zone.Left)
            .Add("rz-spreadsheet-selection-range-bottom", zone.Bottom)
            .Add("rz-spreadsheet-selection-range-right", zone.Right)
            .Add("rz-spreadsheet-frozen-row", zone.FrozenRow)
            .Add("rz-spreadsheet-frozen-column", zone.FrozenColumn)
            .ToString();
    }

    private string GetHandleStyle(SheetChart chart, RangeRef chartRange, RangeInfo zone, string direction)
    {
        var (chartWidth, chartHeight) = GetChartDimensions(chart, chartRange);
        var (offsetX, offsetY) = GetZoneOffset(chart, zone);

        const double half = 4;
        double x = 0, y = 0;

        switch (direction)
        {
            case "nw": x = offsetX - half; y = offsetY - half; break;
            case "n": x = offsetX + chartWidth / 2 - half; y = offsetY - half; break;
            case "ne": x = offsetX + chartWidth - half; y = offsetY - half; break;
            case "e": x = offsetX + chartWidth - half; y = offsetY + chartHeight / 2 - half; break;
            case "se": x = offsetX + chartWidth - half; y = offsetY + chartHeight - half; break;
            case "s": x = offsetX + chartWidth / 2 - half; y = offsetY + chartHeight - half; break;
            case "sw": x = offsetX - half; y = offsetY + chartHeight - half; break;
            case "w": x = offsetX - half; y = offsetY + chartHeight / 2 - half; break;
        }

        return $"position: absolute; left: {x.ToPx()}; top: {y.ToPx()};";
    }

    private void OnChartPointerDown(PointerEventArgs e, SheetChart chart)
    {
        Worksheet.SelectedChart = chart;
        Worksheet.Selection.Clear();
        StateHasChanged();
    }

    private static bool IsStrokeSeries(SpreadsheetChartType chartType) =>
        chartType is SpreadsheetChartType.Line or SpreadsheetChartType.Scatter;

    private RenderFragment RenderChart(SheetChart chart, RangeRef chartRange) => builder =>
    {
        var (chartWidth, chartHeight) = GetChartDimensions(chart, chartRange);
        var dataPoints = GetOrResolveSeriesData(chart);

        builder.OpenComponent<RadzenChart>(0);
        builder.AddAttribute(1, "Style", $"width: {chartWidth.ToPx()}; height: {chartHeight.ToPx()};");
        builder.AddAttribute(2, "ChildContent", (RenderFragment)(chartBuilder =>
        {
            for (var i = 0; i < dataPoints.Count; i++)
            {
                var (series, data) = dataPoints[i];

                var componentType = GetSeriesComponentType(chart.ChartType);
                if (componentType is null)
                {
                    continue;
                }

                var seriesBase = i * 100;

                chartBuilder.OpenComponent(seriesBase, componentType);
                chartBuilder.AddAttribute(seriesBase + 1, "Data", data);
                chartBuilder.AddAttribute(seriesBase + 2, "CategoryProperty", "Category");
                chartBuilder.AddAttribute(seriesBase + 3, "ValueProperty", "Value");
                chartBuilder.AddAttribute(seriesBase + 4, "Title", series.Title ?? "");

                if (series.Color is not null)
                {
                    var colorAttr = IsStrokeSeries(chart.ChartType) ? "Stroke" : "Fill";
                    chartBuilder.AddAttribute(seriesBase + 5, colorAttr, series.Color);
                }

                chartBuilder.CloseComponent();
            }

            // Add axes for non-pie/donut charts
            if (chart.ChartType != SpreadsheetChartType.Pie && chart.ChartType != SpreadsheetChartType.Donut)
            {
                chartBuilder.OpenComponent<RadzenCategoryAxis>(10000);
                chartBuilder.CloseComponent();

                chartBuilder.OpenComponent<RadzenValueAxis>(10001);
                chartBuilder.CloseComponent();
            }

            // Add legend if configured
            if (chart.ShowLegend)
            {
                chartBuilder.OpenComponent<RadzenLegend>(10002);
                chartBuilder.AddAttribute(10003, "Position", chart.LegendPosition);
                chartBuilder.CloseComponent();
            }
        }));
        builder.CloseComponent();
    };

    [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
    private static Type? GetSeriesComponentType(SpreadsheetChartType chartType)
    {
        return chartType switch
        {
            SpreadsheetChartType.Column => typeof(RadzenColumnSeries<ChartDataPoint>),
            SpreadsheetChartType.Bar => typeof(RadzenBarSeries<ChartDataPoint>),
            SpreadsheetChartType.StackedColumn => typeof(RadzenStackedColumnSeries<ChartDataPoint>),
            SpreadsheetChartType.StackedBar => typeof(RadzenStackedBarSeries<ChartDataPoint>),
            SpreadsheetChartType.FullStackedColumn => typeof(RadzenFullStackedColumnSeries<ChartDataPoint>),
            SpreadsheetChartType.FullStackedBar => typeof(RadzenFullStackedBarSeries<ChartDataPoint>),
            SpreadsheetChartType.Line => typeof(RadzenLineSeries<ChartDataPoint>),
            SpreadsheetChartType.Area => typeof(RadzenAreaSeries<ChartDataPoint>),
            SpreadsheetChartType.StackedArea => typeof(RadzenStackedAreaSeries<ChartDataPoint>),
            SpreadsheetChartType.FullStackedArea => typeof(RadzenFullStackedAreaSeries<ChartDataPoint>),
            SpreadsheetChartType.Pie => typeof(RadzenPieSeries<ChartDataPoint>),
            SpreadsheetChartType.Donut => typeof(RadzenDonutSeries<ChartDataPoint>),
            SpreadsheetChartType.Scatter => typeof(RadzenScatterSeries<ChartDataPoint>),
            _ => null
        };
    }

    private List<(ChartSeriesDefinition series, List<ChartDataPoint> data)> GetOrResolveSeriesData(SheetChart chart)
    {
        if (seriesCache.TryGetValue(chart, out var cached))
        {
            return cached;
        }

        var result = new List<(ChartSeriesDefinition, List<ChartDataPoint>)>();

        foreach (var series in chart.Series)
        {
            var data = ChartDataResolver.ResolveSeriesData(series, Worksheet.Workbook, Worksheet);
            result.Add((series, data));
        }

        seriesCache[chart] = result;

        return result;
    }
}
