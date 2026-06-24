namespace Radzen.Blazor
{
    /// <summary>
    /// Specifies where data labels render relative to their data point. Used by <see cref="RadzenSeriesDataLabels" />.
    /// </summary>
    public enum DataLabelPosition
    {
        /// <summary>
        /// The position best suited for the series type (above points for line and area series, outside the end of
        /// columns and bars, centered for stacked segments), flipping when the label would clip the plot edge.
        /// </summary>
        Auto,
        /// <summary>
        /// Above the data point.
        /// </summary>
        Top,
        /// <summary>
        /// Below the data point.
        /// </summary>
        Bottom,
        /// <summary>
        /// Inside the data point's shape (columns, bars, stacked segments).
        /// </summary>
        Inside,
        /// <summary>
        /// Centered on the data point.
        /// </summary>
        Center
    }
}
