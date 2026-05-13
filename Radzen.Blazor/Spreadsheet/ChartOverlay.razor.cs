using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
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
    private readonly Dictionary<SheetChart, List<(ChartSeries series, List<ChartDataPoint> data)>> seriesCache = [];

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

    /// <summary>
    /// Gets or sets the spreadsheet instance.
    /// </summary>
    [CascadingParameter]
    public ISpreadsheet? Spreadsheet { get; set; }

    [Inject]
    ContextMenuService ContextMenuService { get; set; } = default!;

    [Inject]
    DialogService DialogService { get; set; } = default!;

    [Inject]
    Localizer Localizer { get; set; } = default!;

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

        var endCol = chart.From.Column;
        var remaining = chart.Width - (Context.GetRectangle(chart.From.Row, chart.From.Column).Width - chart.From.ColumnOffset);
        while (remaining > 0 && endCol < Worksheet.ColumnCount - 1)
        {
            endCol++;
            remaining -= Context.GetRectangle(chart.From.Row, endCol).Width;
        }

        var endRow = chart.From.Row;
        remaining = chart.Height - (Context.GetRectangle(chart.From.Row, chart.From.Column).Height - chart.From.RowOffset);
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
                fullRect.Width + chart.From.ColumnOffset + chart.To.ColumnOffset,
                fullRect.Height + chart.From.RowOffset + chart.To.RowOffset);
        }

        return (chart.Width, chart.Height);
    }

    private (double x, double y) GetZoneOffset(SheetChart chart, RangeInfo zone)
    {
        double offsetX = chart.From.ColumnOffset;
        double offsetY = chart.From.RowOffset;

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

    private string L(string key) => Localizer.Get(key, System.Globalization.CultureInfo.CurrentUICulture);

    private void OnChartContextMenu(MouseEventArgs e, SheetChart chart)
    {
        Worksheet.SelectedChart = chart;
        Worksheet.Selection.Clear();

        ContextMenuService.Open(e, new List<ContextMenuItem>
        {
            new() { Text = L(nameof(RadzenStrings.Spreadsheet_EditChart)), Value = "edit-chart", Icon = "edit" },
            new() { Text = L(nameof(RadzenStrings.Spreadsheet_DeleteChart)), Value = "delete-chart", Icon = "delete" },
        }, args => OnChartContextMenuItemClick(args, chart));

        StateHasChanged();
    }

    private void OnChartContextMenuItemClick(MenuItemEventArgs args, SheetChart chart)
    {
        ContextMenuService.Close();

        switch (args.Value?.ToString())
        {
            case "edit-chart":
                _ = InvokeAsync(() => OpenEditChartDialogAsync(chart));
                break;
            case "delete-chart":
                Spreadsheet?.Execute(new DeleteChartCommand(Worksheet, chart));
                break;
        }

        StateHasChanged();
    }

    private async Task OpenEditChartDialogAsync(SheetChart chart)
    {
        if (Spreadsheet is not RadzenSpreadsheet spreadsheet)
        {
            return;
        }

        var draft = new EditChartDraft
        {
            ChartType = chart.ChartType,
            Title = chart.Title,
            ShowLegend = chart.ShowLegend,
            LegendPosition = chart.LegendPosition,
            Series = chart.Series.Select((s, i) => new EditChartSeriesDraft
            {
                Index = i,
                Name = s.Name,
                Color = s.Color,
                Categories = s.Categories,
                Values = s.Values,
            }).ToList(),
        };

        string? focusFieldId = null;
        var title = L(nameof(RadzenStrings.Spreadsheet_EditChartTitle));
        object? result;

        // Loop only while the dialog closes for a range-pick request. On OK
        // (EditChartDraft) or Cancel (null) the dialog returns and we exit.
        do
        {
            var parameters = new Dictionary<string, object?>
            {
                [nameof(EditChartDialog.Draft)]        = draft,
                [nameof(EditChartDialog.FocusFieldId)] = focusFieldId,
            };

            result = await DialogService.OpenAsync<EditChartDialog>(
                title,
                parameters,
                new DialogOptions { Width = "480px" });

            if (result is RangePickRequest pick)
            {
                var picked = await spreadsheet.BeginRangePickAsync(pick.Value);
                if (picked != null)
                {
                    ApplyPickedFormula(draft, pick.FieldId, picked);
                }
                focusFieldId = pick.FieldId;
            }
        }
        while (result is RangePickRequest);

        if (result is EditChartDraft finalDraft)
        {
            var newState = new EditChartState(
                finalDraft.ChartType,
                finalDraft.Title,
                finalDraft.ShowLegend,
                finalDraft.LegendPosition,
                finalDraft.Series
                    .Select(s => new EditChartSeriesState(s.Name, s.Color, s.Categories, s.Values))
                    .ToList());

            Spreadsheet?.Execute(new EditChartCommand(chart, newState));
            seriesCache.Remove(chart);
            StateHasChanged();
        }
    }

    private static void ApplyPickedFormula(EditChartDraft draft, string fieldId, string picked)
    {
        const string categoryPrefix = "category_";
        const string valuePrefix = "value_";

        if (fieldId.StartsWith(categoryPrefix, System.StringComparison.Ordinal)
            && int.TryParse(fieldId[categoryPrefix.Length..], out var catIndex)
            && catIndex >= 0 && catIndex < draft.Series.Count)
        {
            draft.Series[catIndex].Categories = picked;
        }
        else if (fieldId.StartsWith(valuePrefix, System.StringComparison.Ordinal)
            && int.TryParse(fieldId[valuePrefix.Length..], out var valIndex)
            && valIndex >= 0 && valIndex < draft.Series.Count)
        {
            draft.Series[valIndex].Values = picked;
        }
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
                chartBuilder.AddAttribute(seriesBase + 4, "Title", series.Name ?? "");

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
                chartBuilder.AddAttribute(10003, "Position", MapLegendPosition(chart.LegendPosition));
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

    private static LegendPosition MapLegendPosition(ChartLegendPosition position)
    {
        return position switch
        {
            ChartLegendPosition.Top => LegendPosition.Top,
            ChartLegendPosition.Bottom => LegendPosition.Bottom,
            ChartLegendPosition.Left => LegendPosition.Left,
            ChartLegendPosition.Right => LegendPosition.Right,
            _ => LegendPosition.Right
        };
    }

    private List<(ChartSeries series, List<ChartDataPoint> data)> GetOrResolveSeriesData(SheetChart chart)
    {
        if (seriesCache.TryGetValue(chart, out var cached))
        {
            return cached;
        }

        var result = new List<(ChartSeries, List<ChartDataPoint>)>();

        foreach (var series in chart.Series)
        {
            var data = ChartDataResolver.ResolveSeriesData(series, Worksheet.Workbook, Worksheet);
            result.Add((series, data));
        }

        seriesCache[chart] = result;

        return result;
    }
}
