using Radzen.Blazor;

namespace Radzen;

/// <summary>
/// Supplies information about a <see cref="RadzenChart.SeriesClick" /> event that is being raised.
/// </summary>
public class SeriesClickEventArgs
{
    /// <summary>
    /// Gets the data at the clicked location.
    /// </summary>
    public object Data { get; set; }

    /// <summary>
    /// Gets the value of the clicked location. Determined by <see cref="CartesianSeries{TItem}.ValueProperty" />.
    /// </summary>
    /// <value>The value.</value>
    public object Value { get; set; }

    /// <summary>
    /// Gets the category of the clicked location. Determined by <see cref="CartesianSeries{TItem}.CategoryProperty" />.
    /// </summary>
    /// <value>The category.</value>
    public object Category { get; set; }

    /// <summary>
    /// Gets the title of the clicked series. Determined by <see cref="CartesianSeries{TItem}.Title" />.
    /// </summary>
    /// <value>The title.</value>
    public string Title { get; set; }

    /// <summary>
    /// Gets the clicked point in axis coordinates.
    /// </summary>
    public SeriesPoint Point { get; set; }
}

