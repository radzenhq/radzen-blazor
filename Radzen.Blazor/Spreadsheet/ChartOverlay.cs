using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Radzen.Blazor.Rendering;
using Radzen.Documents.Spreadsheet;

#nullable enable
namespace Radzen.Blazor.Spreadsheet;

/// <summary>
/// Renders floating charts on a spreadsheet sheet.
/// </summary>
public class ChartOverlay : DrawingOverlayBase<SheetChart>
{
    private readonly Dictionary<SheetChart, List<(ChartSeries series, List<ChartDataPoint> data)>> seriesCache = [];

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

    private readonly EventBinding<Worksheet> worksheetBinding;
    private Worksheet? previousWorksheet;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChartOverlay"/> class.
    /// </summary>
    public ChartOverlay()
    {
        worksheetBinding = new EventBinding<Worksheet>(
            w =>
            {
                w.ChartsChanged += OnChartsChanged;
                w.SelectedChartChanged += OnChanged;
                w.DrawingGeometryChanged += OnDrawingGeometryChanged;
            },
            w =>
            {
                w.ChartsChanged -= OnChartsChanged;
                w.SelectedChartChanged -= OnChanged;
                w.DrawingGeometryChanged -= OnDrawingGeometryChanged;
            });
    }

    /// <inheritdoc/>
    protected override void OnParametersSet()
    {
        worksheetBinding.Bind(Worksheet);

        if (previousWorksheet != Worksheet)
        {
            seriesCache.Clear();
            previousWorksheet = Worksheet;
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

    /// <inheritdoc/>
    public override void Dispose()
    {
        seriesCache.Clear();
        worksheetBinding.Dispose();
        base.Dispose();
    }

    /// <inheritdoc/>
    protected override IEnumerable<SheetChart> Drawings => Worksheet.Charts;

    /// <inheritdoc/>
    protected override bool IsSelected(SheetChart drawing) => Worksheet.SelectedChart == drawing;

    /// <inheritdoc/>
    protected override string BaseCssClass => "rz-spreadsheet-chart";

    /// <inheritdoc/>
    protected override bool HasContextMenu => true;

    /// <inheritdoc/>
    protected override void SelectDrawing(SheetChart drawing)
    {
        Worksheet.SelectedChart = drawing;
    }

    /// <inheritdoc/>
    protected override void RenderInner(RenderTreeBuilder builder, SheetChart drawing, RangeRef range)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(drawing);

        if (drawing.ChartType != SpreadsheetChartType.Unsupported)
        {
            RenderChart(drawing, range)(builder);
        }
        else
        {
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "class", "rz-spreadsheet-chart-unsupported");
            builder.AddContent(2, "Unsupported chart type");
            builder.CloseElement();
        }
    }

    /// <inheritdoc/>
    protected override void OnDrawingContextMenu(MouseEventArgs e, SheetChart drawing)
    {
        Worksheet.SelectedChart = drawing;
        Worksheet.Selection.Clear();

        ContextMenuService.Open(e, new List<ContextMenuItem>
        {
            new() { Text = L(nameof(RadzenStrings.Spreadsheet_EditChart)), Value = "edit-chart", Icon = "edit" },
            new() { Text = L(nameof(RadzenStrings.Spreadsheet_DeleteChart)), Value = "delete-chart", Icon = "delete" },
        }, args => OnChartContextMenuItemClick(args, drawing));

        StateHasChanged();
    }

    private string L(string key) => Localizer.Get(key, Spreadsheet?.UICulture ?? System.Globalization.CultureInfo.CurrentUICulture);

    private void OnChartContextMenuItemClick(MenuItemEventArgs args, SheetChart chart)
    {
        ContextMenuService.Close();

        switch (args.Value?.ToString())
        {
            case "edit-chart":
                _ = InvokeAsync(() => OpenEditChartDialogAsync(chart));
                break;
            case "delete-chart":
                _ = InvokeAsync(async () =>
                {
                    if (Spreadsheet is not null)
                    {
                        await Spreadsheet.ExecuteAsync(new DeleteChartCommand(Worksheet, chart));
                    }
                });
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

            if (Spreadsheet is not null)
            {
                await Spreadsheet.ExecuteAsync(new EditChartCommand(chart, newState));
            }
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
        var (chartWidth, chartHeight) = GetDimensions(chart, chartRange);
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

            if (chart.ChartType != SpreadsheetChartType.Pie && chart.ChartType != SpreadsheetChartType.Donut)
            {
                chartBuilder.OpenComponent<RadzenCategoryAxis>(10000);
                chartBuilder.CloseComponent();

                chartBuilder.OpenComponent<RadzenValueAxis>(10001);
                chartBuilder.CloseComponent();
            }

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
