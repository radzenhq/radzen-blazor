using System;
using Radzen.Blazor.Rendering;
using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    /// <summary>
    /// A timeline component for displaying chronological sequences of events with visual indicators and connecting lines.
    /// RadzenTimeline presents events along a vertical or horizontal axis, ideal for histories, project milestones, or process flows.
    /// Visualizes temporal data in a linear sequence with customizable markers, labels, and content for each event.
    /// Supports Vertical (top-to-bottom) or Horizontal (left-to-right) orientation, Center/Start/End/Alternate positioning of the connecting line,
    /// custom point markers/labels/content per item via templates, content alignment control (start, center, end, stretch), chronological order reversal,
    /// and flexible content where each item can have point marker, label, and main content.
    /// Timeline items are defined using RadzenTimelineItem components. Common uses include order tracking, project progress, changelog displays, or activity feeds.
    /// </summary>
    /// <example>
    /// Basic vertical timeline:
    /// <code>
    /// &lt;RadzenTimeline&gt;
    ///     &lt;Items&gt;
    ///         &lt;RadzenTimelineItem&gt;
    ///             &lt;PointContent&gt;&lt;RadzenIcon Icon="check_circle" /&gt;&lt;/PointContent&gt;
    ///             &lt;ChildContent&gt;
    ///                 &lt;RadzenText TextStyle="TextStyle.H6"&gt;Order Placed&lt;/RadzenText&gt;
    ///                 &lt;RadzenText&gt;January 1, 2025&lt;/RadzenText&gt;
    ///             &lt;/ChildContent&gt;
    ///         &lt;/RadzenTimelineItem&gt;
    ///         &lt;RadzenTimelineItem&gt;
    ///             &lt;PointContent&gt;&lt;RadzenIcon Icon="local_shipping" /&gt;&lt;/PointContent&gt;
    ///             &lt;ChildContent&gt;
    ///                 &lt;RadzenText TextStyle="TextStyle.H6"&gt;Shipped&lt;/RadzenText&gt;
    ///                 &lt;RadzenText&gt;January 2, 2025&lt;/RadzenText&gt;
    ///             &lt;/ChildContent&gt;
    ///         &lt;/RadzenTimelineItem&gt;
    ///     &lt;/Items&gt;
    /// &lt;/RadzenTimeline&gt;
    /// </code>
    /// Horizontal timeline with labels:
    /// <code>
    /// &lt;RadzenTimeline Orientation="Orientation.Horizontal" LinePosition="LinePosition.Bottom"&gt;
    ///     &lt;Items&gt;
    ///         &lt;RadzenTimelineItem&gt;
    ///             &lt;LabelContent&gt;2020&lt;/LabelContent&gt;
    ///             &lt;ChildContent&gt;Company founded&lt;/ChildContent&gt;
    ///         &lt;/RadzenTimelineItem&gt;
    ///         &lt;RadzenTimelineItem&gt;
    ///             &lt;LabelContent&gt;2022&lt;/LabelContent&gt;
    ///             &lt;ChildContent&gt;First product launch&lt;/ChildContent&gt;
    ///         &lt;/RadzenTimelineItem&gt;
    ///     &lt;/Items&gt;
    /// &lt;/RadzenTimeline&gt;
    /// </code>
    /// </example>
    public partial class RadzenTimeline : RadzenComponent
    {
        /// <summary>
        /// Gets or sets the render fragment containing RadzenTimelineItem components that define the timeline events.
        /// Each RadzenTimelineItem represents one event or milestone in the timeline.
        /// </summary>
        /// <value>The items render fragment containing timeline event definitions.</value>
        [Parameter]
        public RenderFragment Items { get; set; }

        /// <summary>
        /// Gets or sets the layout direction of the timeline.
        /// Vertical displays events top-to-bottom, Horizontal displays events left-to-right.
        /// </summary>
        /// <value>The orientation. Default is <see cref="Orientation.Vertical"/>.</value>
        [Parameter]
        public Orientation Orientation { get; set; } = Orientation.Vertical;

        /// <summary>
        /// Gets or sets where the connecting line appears relative to the timeline items.
        /// Options include Center (line between content), Start/End (line on side), or Alternate (zigzag pattern).
        /// </summary>
        /// <value>The line position. Default is <see cref="LinePosition.Center"/>.</value>
        [Parameter]
        public LinePosition LinePosition { get; set; } = LinePosition.Center;

        /// <summary>
        /// Gets or sets whether to reverse the timeline order visually (but not in markup).
        /// When true with vertical orientation, items flow bottom-to-top. With horizontal, items flow right-to-left.
        /// </summary>
        /// <value><c>true</c> to reverse the visual order; otherwise, <c>false</c>. Default is <c>false</c>.</value>
        [Parameter]
        public bool Reverse { get; set; }

        /// <summary>
        /// Gets or sets the cross-axis alignment of timeline item content (label, point, and child content).
        /// Controls vertical alignment for horizontal timelines, or horizontal alignment for vertical timelines.
        /// </summary>
        /// <value>The alignment. Default is <see cref="AlignItems.Center"/>.</value>
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