using System.Collections.Generic;

using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet;

#nullable enable

/// <summary>
/// Represents the editable state of a single chart series.
/// </summary>
public record EditChartSeriesState(string? Color, string? CategoryFormula, string? ValueFormula);

/// <summary>
/// Represents the editable state of a chart.
/// </summary>
public record EditChartState(
    SpreadsheetChartType ChartType,
    string? Title,
    bool ShowLegend,
    ChartLegendPosition LegendPosition,
    List<EditChartSeriesState> SeriesStates
);

/// <summary>
/// Command that edits chart properties and supports undo.
/// </summary>
public class EditChartCommand(SheetChart chart, EditChartState newState) : ICommand
{
    private SpreadsheetChartType oldChartType;
    private string? oldTitle;
    private bool oldShowLegend;
    private ChartLegendPosition oldLegendPosition;
    private readonly List<EditChartSeriesState> oldSeriesStates = [];
    private string? oldRawChartXml;

    /// <inheritdoc/>
    public bool Execute()
    {
        oldChartType = chart.ChartType;
        oldTitle = chart.Title;
        oldShowLegend = chart.ShowLegend;
        oldLegendPosition = chart.LegendPosition;
        oldRawChartXml = chart.RawChartXml;

        oldSeriesStates.Clear();
        foreach (var s in chart.Series)
        {
            oldSeriesStates.Add(new EditChartSeriesState(s.Color, s.CategoryFormula, s.ValueFormula));
        }

        chart.ChartType = newState.ChartType;
        chart.Title = newState.Title;
        chart.ShowLegend = newState.ShowLegend;
        chart.LegendPosition = newState.LegendPosition;
        chart.RawChartXml = null;

        for (int i = 0; i < chart.Series.Count && i < newState.SeriesStates.Count; i++)
        {
            chart.Series[i].Color = newState.SeriesStates[i].Color;
            chart.Series[i].CategoryFormula = newState.SeriesStates[i].CategoryFormula;
            chart.Series[i].ValueFormula = newState.SeriesStates[i].ValueFormula;
        }

        return true;
    }

    /// <inheritdoc/>
    public void Unexecute()
    {
        chart.ChartType = oldChartType;
        chart.Title = oldTitle;
        chart.ShowLegend = oldShowLegend;
        chart.LegendPosition = oldLegendPosition;
        chart.RawChartXml = oldRawChartXml;

        for (int i = 0; i < chart.Series.Count && i < oldSeriesStates.Count; i++)
        {
            chart.Series[i].Color = oldSeriesStates[i].Color;
            chart.Series[i].CategoryFormula = oldSeriesStates[i].CategoryFormula;
            chart.Series[i].ValueFormula = oldSeriesStates[i].ValueFormula;
        }
    }
}
