using System;
using Radzen.Blazor.Rendering;
using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenTimeline component is a graphical representation used to display a chronological sequence of events or data points.
    /// </summary>
    /// <example>
    /// <code>
    /// &lt;RadzenTimeline&gt;
    /// &lt;Items&gt;
    ///     &lt;RadzenTimelineItem&gt;
    ///         &lt;ChildContent&gt;
    ///             Checkpoint 1
    ///         &lt;/ChildContent&gt;
    ///     &lt;/RadzenTimelineItem&gt;
    ///     &lt;RadzenTimelineItem&gt;
    ///         &lt;ChildContent&gt;
    ///             Checkpoint 2
    ///         &lt;/ChildContent&gt;
    ///     &lt;/RadzenTimelineItem&gt;
    /// &lt;/Items&gt;
    /// &lt;/RadzenTimeline&gt;
    /// </code>
    /// </example>
    public partial class RadzenTimeline : RadzenComponent
    {
        /// <summary>
        /// Gets or sets the items.
        /// </summary>
        /// <value>The items.</value>
        [Parameter]
        public RenderFragment Items { get; set; }

        /// <summary>
        /// Specifies the orientation - whether items flow in horizontal or vertical direction. Set to <c>Orientation.Vertical</c> by default.
        /// </summary>
        [Parameter]
        public Orientation Orientation { get; set; } = Orientation.Vertical;

        /// <summary>
        /// Specifies the line position. Set to <c>LinePosition.Center</c> by default.
        /// </summary>
        [Parameter]
        public LinePosition LinePosition { get; set; } = LinePosition.Center;

        /// <summary>
        /// Specifies if the LinePosition is reversed.
        /// </summary>
        /// <value><c>true</c> if reverse; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool Reverse { get; set; }

        /// <summary>
        /// Specifies the alignment of LabelContent, PointContent and ChildContent inside TimelineItems. Set to <c>AlignItems.Center</c> by default.
        /// </summary>
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