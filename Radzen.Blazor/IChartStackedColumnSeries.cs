namespace Radzen.Blazor
{
    /// <summary>
    /// Marker interface for <see cref="RadzenStackedColumnSeries{TItem}" />.
    /// </summary>
    public interface IChartStackedColumnSeries
    {
        /// <summary>
        /// Gets the count.
        /// </summary>
        /// <value>The count.</value>
        int Count { get; }

        /// <summary>
        /// Gets maximum value.
        /// </summary>
        double Max { get; }

        /// <summary>
        /// Gets the value at the specified index.
        /// </summary>
        double ValueAt(int index);
    }
}