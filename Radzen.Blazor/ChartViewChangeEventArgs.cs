namespace Radzen;

/// <summary>
/// Supplies information about a <see cref="Blazor.RadzenChart.ViewChange" /> event that is being raised.
/// </summary>
public class ChartViewChangeEventArgs
{
    /// <summary>
    /// Gets the current zoom level as a percentage. 100 means no zoom (full range visible).
    /// </summary>
    public double Zoom { get; set; }

    /// <summary>
    /// Gets the start of the visible range as a fraction (0-1) of the full category range.
    /// </summary>
    public double ViewStart { get; set; }

    /// <summary>
    /// Gets the end of the visible range as a fraction (0-1) of the full category range.
    /// </summary>
    public double ViewEnd { get; set; }
}
