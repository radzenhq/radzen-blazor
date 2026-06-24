namespace Radzen.Blazor
{
    /// <summary>
    /// Specifies how data is positioned relative to the ticks of a category axis, which controls the
    /// empty space reserved at the start and end of the axis. Set via <c>RadzenCategoryAxis.TickPlacement</c>.
    /// </summary>
    public enum TickPlacement
    {
        /// <summary>
        /// Each category is centered between ticks, reserving half a category band before the first
        /// point and after the last. This is the default.
        /// </summary>
        Between,
        /// <summary>
        /// Each category sits on its tick, so the first and last points are flush with the plot edges
        /// and no space is reserved.
        /// </summary>
        On
    }
}
