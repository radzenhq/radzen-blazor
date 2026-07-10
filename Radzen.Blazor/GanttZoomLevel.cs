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
        /// Year-level scale (one cell per month). Honors ViewStart and ViewEnd and can span multiple years.
        /// </summary>
        Year,

        /// <summary>
        /// Multi-year scale (one cell per quarter, grouped by year) for the widest ranges.
        /// </summary>
        Years
    }
}

