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
        public PointSize Size { get; set; } = PointSize.Medium;

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
        public Variant Variant { get; set; } = Variant.Filled;

        /// <summary>
        /// Gets or sets the Shadow level.
        /// </summary>
        /// <value>The point shadow level.</value>
        [Parameter]
        public int Shadow { get; set; } = 1;

        private string PointClass => ClassList.Create($"rz-timeline-point")
                                .Add("rz-timeline-point-xs", Size == PointSize.ExtraSmall)
                                .Add("rz-timeline-point-sm", Size == PointSize.Small)
                                .Add("rz-timeline-point-md", Size == PointSize.Medium)
                                .Add("rz-timeline-point-lg", Size == PointSize.Large)
                                .Add("rz-timeline-point-filled", Variant == Variant.Filled)
                                .Add("rz-timeline-point-flat", Variant == Variant.Flat)
                                .Add("rz-timeline-point-outlined", Variant == Variant.Outlined)
                                .Add("rz-timeline-point-text", Variant == Variant.Text)
                                .Add("rz-shadow-0", Shadow == 0)
                                .Add("rz-shadow-1", Shadow == 1)
                                .Add("rz-shadow-2", Shadow == 2)
                                .Add("rz-shadow-3", Shadow == 3)
                                .Add("rz-shadow-4", Shadow == 4)
                                .Add("rz-shadow-5", Shadow == 5)
                                .Add("rz-shadow-6", Shadow == 6)
                                .Add("rz-shadow-7", Shadow == 7)
                                .Add("rz-shadow-8", Shadow == 8)
                                .Add("rz-shadow-9", Shadow == 9)
                                .Add("rz-shadow-10", Shadow == 10)
                                .Add($"rz-timeline-point-{PointStyle.ToString().ToLowerInvariant()}").ToString();

        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            return ClassList.Create($"rz-timeline-item").ToString();
        }
    }
}