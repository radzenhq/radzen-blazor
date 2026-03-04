using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    /// <summary>
    /// Supplies information about a <see cref="RadzenGantt{TItem}.TaskMouseEnter" /> or <see cref="RadzenGantt{TItem}.TaskMouseLeave" /> event that is being raised.
    /// </summary>
    /// <typeparam name="TItem">The type of the data item.</typeparam>
    public class GanttTaskMouseEventArgs<TItem>
    {
        /// <summary>
        /// A reference to the DOM element of the task bar that triggered the event.
        /// </summary>
        public ElementReference Element { get; set; }

        /// <summary>
        /// The data item for which the task bar is created.
        /// </summary>
        /// <value>The data.</value>
        public TItem? Data { get; set; }

        /// <summary>
        /// The horizontal position (X) of the mouse pointer in viewport coordinates.
        /// </summary>
        public double ClientX { get; set; }

        /// <summary>
        /// The vertical position (Y) of the mouse pointer in viewport coordinates.
        /// </summary>
        public double ClientY { get; set; }
    }
}
