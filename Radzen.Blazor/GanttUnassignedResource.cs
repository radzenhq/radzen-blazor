namespace Radzen.Blazor
{
    /// <summary>
    /// Row item representing the "Unassigned" bucket in the <see cref="RadzenGantt{TItem}"/> resource view.
    /// Rendered when <c>ShowUnassignedTasks</c> is enabled and tasks reference no known resource.
    /// Custom resource column templates can type-check for this class to render the bucket row.
    /// </summary>
    public sealed class GanttUnassignedResource
    {
        /// <summary>
        /// Gets the display text of the unassigned tasks row.
        /// </summary>
        public string Text { get; internal set; } = string.Empty;

        /// <inheritdoc />
        public override string ToString() => Text;
    }
}
