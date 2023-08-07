using System;
using Radzen.Blazor.Rendering;
using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    /// <summary>
    /// </summary>
    /// <example>
    /// <code>
    /// </code>
    /// </example>
    public partial class RadzenTimeline : RadzenComponent
    {
        [Parameter]
        public RenderFragment Items { get; set; }

        /// <summary>
        /// Gets or sets the orientation.
        /// </summary>
        /// <value>The orientation.</value>
        [Parameter]
        public Orientation Orientation { get; set; } = Orientation.Vertical;

        /// <summary>
        /// Gets or sets the position.
        /// </summary>
        /// <value>The position.</value>
        [Parameter]
        public LinePosition LinePosition { get; set; } = LinePosition.Center;

        /// <summary>
        /// Gets or sets the reverse.
        /// </summary>
        /// <value>The reverse.</value>
        [Parameter]
        public bool Reverse { get; set; }

        /// <summary>
        /// Gets or sets the TimelineItems content alignment.
        /// </summary>
        /// <value>The TimelineItems content alignment.</value>
        [Parameter]
        public AlignItems AlignItems { get; set; } = AlignItems.Center;

        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            return ClassList.Create($"rz-timeline")
                            .Add("rz-timeline-row", Orientation == Orientation.Horizontal)
                            .Add("rz-timeline-column", Orientation == Orientation.Vertical)
                            .Add($"rz-timeline-{LinePosition.ToString().ToLowerInvariant()}")
                            .Add("rz-timeline-reverse", Reverse)
                            .Add($"rz-timeline-align-items-{AlignItems.ToString().ToLowerInvariant()}")
                            .ToString();
        }
    }
}