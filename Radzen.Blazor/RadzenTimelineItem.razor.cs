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

        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            return ClassList.Create($"rz-timeline-item rz-display-flex rz-flex-row rz-align-items-center").ToString();
        }
    }
}