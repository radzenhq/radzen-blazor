namespace Radzen.Blazor
{
    /// <summary>
    /// Specifies which data points display labels. Used by <see cref="RadzenSeriesDataLabels" />.
    /// </summary>
    public enum DataLabelDisplay
    {
        /// <summary>
        /// Every data point displays a label (subject to <see cref="RadzenSeriesDataLabels.Step" /> and overlap hiding).
        /// </summary>
        All,
        /// <summary>
        /// Only the minimum and maximum values display labels.
        /// </summary>
        MinMax,
        /// <summary>
        /// Only the first and last data points display labels.
        /// </summary>
        FirstLast
    }
}
