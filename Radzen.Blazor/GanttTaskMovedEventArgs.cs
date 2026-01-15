using System;

namespace Radzen.Blazor
{
    /// <summary>
    /// Supplies information about a <see cref="RadzenGantt{TItem}.TaskMove" /> or <see cref="RadzenGantt{TItem}.TaskResize" /> event.
    /// </summary>
    /// <typeparam name="TItem">The type of the data item.</typeparam>
    public class GanttTaskMovedEventArgs<TItem>
    {
        /// <summary>
        /// The data item that was moved or resized.
        /// </summary>
        public TItem Data { get; set; } = default!;

        /// <summary>
        /// The new start date after the move or resize.
        /// </summary>
        public DateTime NewStart { get; set; }

        /// <summary>
        /// The new end date after the move or resize.
        /// </summary>
        public DateTime NewEnd { get; set; }
    }
}
