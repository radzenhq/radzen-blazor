using Radzen.Blazor;

namespace Radzen;

/// <summary>
/// Supplies information about a legend item to <see cref="RadzenLegend.ItemTemplate" />.
/// </summary>
public class LegendItemContext
{
    /// <summary>
    /// Gets the text which the legend item displays by default - the series title or the category of the data item.
    /// </summary>
    /// <value>The text.</value>
    public string? Text { get; set; }

    /// <summary>
    /// Gets the data of the legend item. Series that render a legend item per data item (pie, donut, funnel, pyramid) provide the data item.
    /// Heatmap and contour series with a color range provide the <see cref="SeriesColorRange" />.
    /// Other series provide their <see cref="CartesianSeries{TItem}.Data" />.
    /// </summary>
    /// <value>The data.</value>
    public object? Data { get; set; }

    /// <summary>
    /// Gets the series which the legend item belongs to.
    /// </summary>
    /// <value>The series.</value>
    public IChartSeries? Series { get; set; }

    /// <summary>
    /// Gets the color of the legend item marker. It is <c>null</c> when the color comes from the theme and is not set explicitly (e.g. via <c>Fills</c>).
    /// </summary>
    /// <value>The color.</value>
    public string? Color { get; set; }
}
