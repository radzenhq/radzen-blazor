using Radzen.Blazor;

namespace Radzen;

/// <summary>
/// Supplies information about a <see cref="RadzenChart.LegendClick" /> event that is being raised.
/// </summary>
public class LegendClickEventArgs
{
    /// <summary>
    /// Gets the data at the clicked location.
    /// </summary>
    public object Data { get; set; }

    /// <summary>
    /// Gets the title of the clicked series. Determined by <see cref="CartesianSeries{TItem}.Title" />.
    /// </summary>
    /// <value>The title.</value>
    public string Title { get; set; }

    /// <summary>
    /// Gets the visibility of the clicked legend. Determined by <see cref="CartesianSeries{TItem}.IsVisible" />. Always visible for Pie Charts.
    /// </summary>
    /// <value>The visibility.</value>
    public bool IsVisible { get; set; }
}

