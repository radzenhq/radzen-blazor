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
    public partial class RadzenTimelineItem : RadzenComponent
    {
        /// <summary>
        /// Gets or sets the child content.
        /// </summary>
        /// <value>The child content.</value>
        [Parameter]
        public RenderFragment ChildContent { get; set; }

        /// <summary>
        /// Gets or sets the Start content.
        /// </summary>
        /// <value>The Start content.</value>
        [Parameter]
        public RenderFragment Start { get; set; }

        /// <summary>
        /// Gets or sets the End content.
        /// </summary>
        /// <value>The End content.</value>
        [Parameter]
        public RenderFragment End { get; set; }

        /// <summary>
        /// Gets or sets the Point content.
        /// </summary>
        /// <value>The point content.</value>
        [Parameter]
        public RenderFragment Point { get; set; }

        /// <summary>
        /// Gets or sets the Point size.
        /// </summary>
        /// <value>The point size.</value>
        [Parameter]
        public PointSize PointSize { get; set; } = PointSize.Medium;

        /// <summary>
        /// Gets or sets the Point style.
        /// </summary>
        /// <value>The point style.</value>
        [Parameter]
        public PointStyle PointStyle { get; set; } = PointStyle.Base;

        /// <summary>
        /// Gets or sets the Point variant.
        /// </summary>
        /// <value>The point variant.</value>
        [Parameter]
        public Variant PointVariant { get; set; } = Variant.Filled;

        /// <summary>
        /// Gets or sets the Shadow level.
        /// </summary>
        /// <value>The point shadow level.</value>
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