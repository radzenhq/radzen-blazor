using System;
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
        public RenderFragment ChildContent { get; set; }

        /// <summary>
        /// Gets or sets the text.
        /// </summary>
        [Parameter]
        public string Text { get; set; }

        /// <summary>
        /// Gets or sets the label content.
        /// </summary>
        [Parameter]
        public RenderFragment LabelContent { get; set; }

        /// <summary>
        /// Gets or sets the label.
        /// </summary>
        [Parameter]
        public string Label { get; set; }

        /// <summary>
        /// Gets or sets the content inside a point on the timeline.
        /// </summary>
        [Parameter]
        public RenderFragment PointContent { get; set; }

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

        private string PointClass => ClassList.Create($"rz-timeline-point")
                                .Add($"rz-timeline-point-{PointVariant.ToString().ToLowerInvariant()}")
                                .Add($"rz-shadow-{PointShadow.ToString().ToLowerInvariant()}")
                                .Add($"rz-timeline-point-{PointStyle.ToString().ToLowerInvariant()}")
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

            return ClassList.Create($"rz-timeline-item rz-timeline-axis-{pointSizeCSS}").ToString();
        }
    }
}