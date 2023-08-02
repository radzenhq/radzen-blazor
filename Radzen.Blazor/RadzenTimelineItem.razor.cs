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
                                .Add("rz-timeline-point-filled", PointVariant == Variant.Filled)
                                .Add("rz-timeline-point-flat", PointVariant == Variant.Flat)
                                .Add("rz-timeline-point-outlined", PointVariant == Variant.Outlined)
                                .Add("rz-timeline-point-text", PointVariant == Variant.Text)
                                .Add("rz-shadow-0", PointShadow == 0)
                                .Add("rz-shadow-1", PointShadow == 1)
                                .Add("rz-shadow-2", PointShadow == 2)
                                .Add("rz-shadow-3", PointShadow == 3)
                                .Add("rz-shadow-4", PointShadow == 4)
                                .Add("rz-shadow-5", PointShadow == 5)
                                .Add("rz-shadow-6", PointShadow == 6)
                                .Add("rz-shadow-7", PointShadow == 7)
                                .Add("rz-shadow-8", PointShadow == 8)
                                .Add("rz-shadow-9", PointShadow == 9)
                                .Add("rz-shadow-10", PointShadow == 10)
                                .Add($"rz-timeline-point-{PointStyle.ToString().ToLowerInvariant()}").ToString();

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