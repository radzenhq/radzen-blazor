namespace Radzen.Blazor
{
    /// <summary>
    /// Specifies the zoom level (time scale) used by <see cref="RadzenGantt{TItem}"/>.
    /// </summary>
    public enum GanttZoomLevel
    {
        /// <summary>
        /// Day-level scale (one cell per day).
        /// </summary>
        Day,

        /// <summary>
        /// Week-level scale (still renders days, but intended for wider ranges).
        /// </summary>
        Week,

        /// <summary>
        /// Month-level scale (still renders days, but intended for wider ranges).
        /// </summary>
        Month,

        /// <summary>
        /// Year-level scale (still renders days, but intended for the widest ranges).
        /// </summary>
        Year
    }
}

