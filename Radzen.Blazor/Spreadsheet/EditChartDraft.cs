using System.Collections.Generic;
using Radzen.Documents.Spreadsheet;

namespace Radzen.Blazor.Spreadsheet;

#nullable enable

/// <summary>
/// Mutable working state for the Edit Chart dialog. Survives close/reopen
/// cycles when the user engages a range picker on one of the series.
/// </summary>
public sealed class EditChartDraft
{
    /// <summary>Chart type.</summary>
    public SpreadsheetChartType ChartType { get; set; }

    /// <summary>Chart title.</summary>
    public string? Title { get; set; }

    /// <summary>Whether the legend is shown.</summary>
    public bool ShowLegend { get; set; }

    /// <summary>Legend position.</summary>
    public ChartLegendPosition LegendPosition { get; set; }

    /// <summary>Series being edited.</summary>
    public List<EditChartSeriesDraft> Series { get; set; } = new();
}

/// <summary>Mutable working state for a single chart series row in the Edit Chart dialog.</summary>
public sealed class EditChartSeriesDraft
{
    /// <summary>Zero-based ordinal of the series within the chart.</summary>
    public int Index { get; set; }

    /// <summary>Display title.</summary>
    public string? Title { get; set; }

    /// <summary>Series color.</summary>
    public string? Color { get; set; }

    /// <summary>Sheet-qualified absolute formula for categories.</summary>
    public string? CategoryFormula { get; set; }

    /// <summary>Sheet-qualified absolute formula for values.</summary>
    public string? ValueFormula { get; set; }
}
