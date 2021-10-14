namespace Radzen.Blazor
{
    /// <summary>
    /// Marker interface for <see cref="RadzenColumnSeries{TItem}" />.
    /// </summary>
    public interface IChartColumnSeries
    {
        /// <summary>
        /// Gets the count.
        /// </summary>
        /// <value>The count.</value>
        int Count { get; }
    }
}