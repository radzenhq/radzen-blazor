namespace Radzen;

/// <summary>
/// Supplies information about a <see cref="Blazor.RadzenChart.LoadData" /> event that is being raised.
/// </summary>
public class ChartLoadDataEventArgs
{
    /// <summary>
    /// Gets the category value at the start of the visible range, for example a <see cref="System.DateTime" /> when the category axis shows dates.
    /// It is <c>null</c> when the chart has no range yet - the series have no data and the category axis has neither <see cref="Blazor.AxisBase.Min" />
    /// nor <see cref="Blazor.AxisBase.Max" />. Set them to give the chart a range before any data is loaded.
    /// </summary>
    public object? Start { get; set; }

    /// <summary>
    /// Gets the category value at the end of the visible range.
    /// </summary>
    public object? End { get; set; }

    /// <summary>
    /// Gets the start of the visible range as a fraction (0-1) of the full category range.
    /// </summary>
    public double ViewStart { get; set; }

    /// <summary>
    /// Gets the end of the visible range as a fraction (0-1) of the full category range.
    /// </summary>
    public double ViewEnd { get; set; }

    /// <summary>
    /// Gets the width of the plot area in pixels. Use it to decide how many points to load for the visible range.
    /// </summary>
    public double Width { get; set; }
}
