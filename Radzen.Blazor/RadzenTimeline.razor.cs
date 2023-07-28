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
    public partial class RadzenTimeline : RadzenComponent
    {
        [Parameter]
        public RenderFragment Items { get; set; }

        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            return ClassList.Create($"rz-timeline rz-display-flex rz-flex-column").ToString();
        }
    }
}