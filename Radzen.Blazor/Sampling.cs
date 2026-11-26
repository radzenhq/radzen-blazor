namespace Radzen.Blazor
{
    /// <summary>
    /// Specifies how a series reduces the number of points it renders when there are more points than pixels.
    /// Used by <see cref="RadzenLineSeries{TItem}"/> and <see cref="RadzenAreaSeries{TItem}"/>.
    /// </summary>
    public enum Sampling
    {
        /// <summary>
        /// Every point is rendered.
        /// </summary>
        None,
        /// <summary>
        /// Points that share the same half of a horizontal pixel are reduced to the first, the lowest, the highest and the last of them,
        /// so at most eight points per pixel are rendered. A series drawn with straight lines keeps its shape, including its peaks.
        /// </summary>
        MinMax
    }
}
