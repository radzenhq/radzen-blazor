using System;

namespace Radzen.Blazor
{
    /// <summary>
    /// A named vertical line rendered on the Gantt timeline.
    /// </summary>
    public class GanttMarker
    {
        /// <summary>
        /// The date/time at which the marker is drawn.
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// Optional label shown at the top of the marker line.
        /// </summary>
        public string? Label { get; set; }

        /// <summary>
        /// Optional CSS color for the marker line and label. Defaults to the danger color.
        /// </summary>
        public string? Color { get; set; }
    }
}
