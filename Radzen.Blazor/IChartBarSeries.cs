namespace Radzen.Blazor
{
    /// <summary>
    /// Marker interface for <see cref="RadzenBarSeries{TItem}" />.
    /// </summary>
    public interface IChartBarSeries
    {
        /// <summary>
        /// Gets the count.
        /// </summary>
        /// <value>The count.</value>
        int Count { get; }
    }
}