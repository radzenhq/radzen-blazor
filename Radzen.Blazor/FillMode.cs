namespace Radzen.Blazor
{
    /// <summary>
    /// Specifies how a series shape is filled. Used by the area, column and bar series families
    /// (including their stacked, full-stacked, range and bullet variants) and by the pie and donut series.
    /// </summary>
    public enum FillMode
    {
        /// <summary>
        /// The shape is filled with a gradient of the series color which fades towards the axis baseline.
        /// </summary>
        Gradient,
        /// <summary>
        /// The shape is filled with a solid color.
        /// </summary>
        Solid,
        /// <summary>
        /// The shape is not filled - only the outline (or line) is rendered.
        /// </summary>
        None
    }
}
