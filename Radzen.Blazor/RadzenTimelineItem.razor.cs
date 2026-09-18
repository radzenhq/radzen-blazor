using System;
using System.Globalization;
using Radzen.Blazor.Rendering;
using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenTimeline item.
    /// </summary>
    /// <example>
    /// <code>
    /// &lt;RadzenTimelineItem PointStyle="PointStyle.Primary"&gt;
    ///     &lt;LabelContent&gt;
    ///         NOV 2022
    ///     &lt;/LabelContent&gt;
    ///     &lt;ChildContent&gt;
    ///         Celebrating the official release of Radzen Blazor Studio.
    ///     &lt;/ChildContent&gt;
    /// &lt;/RadzenTimelineItem&gt;
    /// </code>
    /// </example>
    public partial class RadzenTimelineItem : RadzenComponent
    {
        /// <summary>
        /// Gets or sets the child content.
        /// </summary>
        [Parameter]
        public RenderFragment? ChildContent { get; set; }

        /// <summary>
        /// Gets or sets the text.
        /// </summary>
        [Parameter]
        public string? Text { get; set; }

        /// <summary>
        /// Gets or sets the label content.
        /// </summary>
        [Parameter]
        public RenderFragment? LabelContent { get; set; }

        /// <summary>
        /// Gets or sets the label.
        /// </summary>
        [Parameter]
        public string? Label { get; set; }

        /// <summary>
        /// Gets or sets the content inside a point on the timeline.
        /// </summary>
        [Parameter]
        public RenderFragment? PointContent { get; set; }

        /// <summary>
        /// Specifies the Point size from ExtraSmall to Large. Set to <c>PointSize.Medium</c> by default.
        /// </summary>
        [Parameter]
        public PointSize PointSize { get; set; } = PointSize.Medium;

        /// <summary>
        /// Gets or sets the Point style. Set to <c>PointStyle.Base</c> by default.
        /// </summary>
        [Parameter]
        public PointStyle PointStyle { get; set; } = PointStyle.Base;

        /// <summary>
        /// Specifies if the Point variant is filled, flat, outlined or text only. Set to <c>Variant.Filled</c> by default.
        /// </summary>
        [Parameter]
        public Variant PointVariant { get; set; } = Variant.Filled;

        /// <summary>
        /// Specifies the Shadow level from <c>0</c> (no shadow) to <c>10</c>. Set to <c>1</c> by default.
        /// </summary>
        [Parameter]
        public int PointShadow { get; set; } = 1;

        /// <summary>
        /// Gets or sets additional CSS classes applied to the point element itself, rather than to the item.
        /// Use it to style the marker beyond what <see cref="PointStyle"/> and <see cref="PointVariant"/> express -
        /// a focus ring on the active step, a transition, an animation.
        /// </summary>
        [Parameter]
        public string? PointClass { get; set; }

        /// <summary>
        /// Gets or sets the style of the connector running from this item to the next one rendered.
        /// The last item has no outgoing connector, so its value is ignored. Leave it <c>null</c> to keep the
        /// theme line colour.
        /// </summary>
        [Parameter]
        public PointStyle? LineStyle { get; set; }

        /// <summary>
        /// Gets or sets whether this item is the step currently in progress. It is marked
        /// <c>aria-current="step"</c> and its point is drawn with a ring in the point's own colour.
        /// At most one item in a timeline should set it.
        /// </summary>
        [Parameter]
        public bool Current { get; set; }

        private string? AriaCurrent => Current ? "step" : null;

        private string PointCssClass => ClassList.Create($"rz-timeline-point")
                                .Add($"rz-timeline-point-{PointVariant.ToString().ToLowerInvariant()}")
                                .Add($"rz-shadow-{PointShadow.ToString(CultureInfo.InvariantCulture).ToLowerInvariant()}")
                                .Add($"rz-timeline-point-{PointStyle.ToString().ToLowerInvariant()}")
                                .Add(PointClass)
                                .ToString();

        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            var pointSizeCSS = "md";

            if (PointSize == PointSize.ExtraSmall)
            {
                pointSizeCSS = "xs";
            }
            else if (PointSize == PointSize.Small)
            {
                pointSizeCSS = "sm";
            }
            else if (PointSize == PointSize.Large)
            {
                pointSizeCSS = "lg";
            }

            return ClassList.Create($"rz-timeline-item rz-timeline-axis-{pointSizeCSS}")
                            .Add($"rz-timeline-line-{LineStyle?.ToString().ToLowerInvariant()}", LineStyle != null)
                            .Add("rz-timeline-item-current", Current)
                            .ToString();
        }
    }
}