namespace Radzen.Blazor
{
    /// <summary>
    /// Specifies how the area of a series is filled. Used by <see cref="RadzenAreaSeries{TItem}"/>.
    /// </summary>
    public enum FillMode
    {
        /// <summary>
        /// The area is filled with a vertical gradient of the series color which fades towards the baseline.
        /// </summary>
        Gradient,
        /// <summary>
        /// The area is filled with a solid color.
        /// </summary>
        Solid,
        /// <summary>
        /// The area is not filled - only the line is rendered.
        /// </summary>
        None
    }
}
