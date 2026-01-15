namespace Radzen.Blazor
{
    /// <summary>
    /// Defines a dependency between two Gantt tasks.
    /// </summary>
    /// <typeparam name="TItem">The task item type.</typeparam>
    public class GanttDependency<TItem>
    {
        /// <summary>
        /// The predecessor task.
        /// </summary>
        public TItem From { get; set; } = default!;

        /// <summary>
        /// The successor task.
        /// </summary>
        public TItem To { get; set; } = default!;

        /// <summary>
        /// The dependency type. Defaults to <see cref="GanttDependencyType.FinishToStart"/>.
        /// </summary>
        public GanttDependencyType Type { get; set; } = GanttDependencyType.FinishToStart;
    }
}
