namespace Radzen.Blazor
{
    /// <summary>
    /// Specifies the view mode used by <see cref="RadzenGantt{TItem}"/>.
    /// </summary>
    public enum GanttViewMode
    {
        /// <summary>
        /// Default task-based view. Rows represent tasks.
        /// </summary>
        Task,

        /// <summary>
        /// Resource-based view. Rows represent resources and task bars are grouped by their assigned resource.
        /// Multiple tasks assigned to the same resource appear as stacked bars within the resource row.
        /// </summary>
        Resource
    }
}
