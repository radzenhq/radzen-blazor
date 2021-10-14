namespace Radzen.Blazor
{
    /// <summary>
    /// Specifies the type of marker that <see cref="RadzenChart" /> displays for data points.
    /// </summary>
    public enum MarkerType
    {
        /// <summary>
        /// Do not display a marker for data points.
        /// </summary>
        None,
        /// <summary>
        /// Cycle between markers.
        /// </summary>
        Auto,
        /// <summary>
        /// Use a circle marker.
        /// </summary>
        Circle,
        /// <summary>
        /// Use a triangle marker.
        /// </summary>
        Triangle,
        /// <summary>
        /// Use a square marker.
        /// </summary>
        Square,
        /// <summary>
        /// Use a diamond marker.
        /// </summary>
        Diamond
    }
}
