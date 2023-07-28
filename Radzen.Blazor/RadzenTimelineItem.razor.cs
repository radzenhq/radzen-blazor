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
        public PointSize Size { get; set; } = PointSize.Medium;

        private string PointClass => ClassList.Create($"rz-timeline-stop")
                                .Add("rz-timeline-stop-xs", Size == PointSize.ExtraSmall)
                                .Add("rz-timeline-stop-sm", Size == PointSize.Small)
                                .Add("rz-timeline-stop-md", Size == PointSize.Medium)
                                .Add("rz-timeline-stop-lg", Size == PointSize.Large)
                                .Add("rz-timeline-stop-filled").ToString();

        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            return ClassList.Create($"rz-timeline-item rz-display-flex rz-flex-row rz-align-items-center").ToString();
        }
    }
}