using System.Collections.Generic;

using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet;

#nullable enable

/// <summary>
/// Represents the editable state of a single chart series.
/// </summary>
public record EditChartSeriesState(string? Name, string? Color, string? Categories, string? Values);

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

    private static List<ChartSeries> ToSeriesList(List<EditChartSeriesState> states)
    {
        var list = new List<ChartSeries>();
        for (int i = 0; i < states.Count; i++)
        {
            list.Add(new ChartSeries
            {
                Name = states[i].Name,
                Color = states[i].Color,
                Categories = states[i].Categories,
                Values = states[i].Values,
                Index = i
            });
        }
        return list;
    }

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
            oldSeriesStates.Add(new EditChartSeriesState(s.Name, s.Color, s.Categories, s.Values));
        }

        chart.ChartType = newState.ChartType;
        chart.Title = newState.Title;
        chart.ShowLegend = newState.ShowLegend;
        chart.LegendPosition = newState.LegendPosition;
        chart.RawChartXml = null;
        chart.Series = ToSeriesList(newState.SeriesStates);

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
        chart.Series = ToSeriesList(oldSeriesStates);
    }
}
