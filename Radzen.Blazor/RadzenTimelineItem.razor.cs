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
        [Parameter]
        public RenderFragment ChildContent { get; set; }

        [Parameter]
        public RenderFragment Start { get; set; }

        [Parameter]
        public RenderFragment End { get; set; }

        [Parameter]
        public RenderFragment Point { get; set; }

        [Parameter]
        public PointSize Size { get; set; } = PointSize.Medium;

        [Parameter]
        public PointStyle PointStyle { get; set; } = PointStyle.Base;

        private string PointClass => ClassList.Create($"rz-timeline-point")
                                .Add("rz-timeline-point-xs", Size == PointSize.ExtraSmall)
                                .Add("rz-timeline-point-sm", Size == PointSize.Small)
                                .Add("rz-timeline-point-md", Size == PointSize.Medium)
                                .Add("rz-timeline-point-lg", Size == PointSize.Large)
                                .Add($"rz-timeline-point-{PointStyle.ToString().ToLowerInvariant()}")
                                .Add("rz-timeline-point-filled").ToString();

        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            return ClassList.Create($"rz-timeline-item rz-display-flex rz-flex-row rz-align-items-center").ToString();
        }
    }
}