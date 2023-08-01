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

        /// <summary>
        /// Gets or sets the orientation.
        /// </summary>
        /// <value>The orientation.</value>
        [Parameter]
        public Orientation Orientation { get; set; } = Orientation.Vertical;

        /// <summary>
        /// Gets or sets the position.
        /// </summary>
        /// <value>The position.</value>
        [Parameter]
        public TimelinePosition TimelinePosition { get; set; } = TimelinePosition.Center;

        /// <summary>
        /// Gets or sets the reverse.
        /// </summary>
        /// <value>The reverse.</value>
        [Parameter]
        public bool Reverse { get; set; }



        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            var horizontal = Orientation == Orientation.Horizontal;
            
            var positionCSS = "rz-timeline-center";

            if (TimelinePosition == TimelinePosition.Alternate)
            {
                positionCSS = "rz-timeline-alternate";
            }
            else if (TimelinePosition == TimelinePosition.Start)
            {
                positionCSS = "rz-timeline-start";
            }
            else if (TimelinePosition == TimelinePosition.End)
            {
                positionCSS = "rz-timeline-end";
            }
            else if(TimelinePosition == TimelinePosition.Left)
            {
                positionCSS = "rz-timeline-left";
            }
            else if (TimelinePosition == TimelinePosition.Right)
            {
                positionCSS = "rz-timeline-right";
            }
            else if(TimelinePosition == TimelinePosition.Top)
            {
                positionCSS = "rz-timeline-top";
            }
            else if (TimelinePosition == TimelinePosition.Bottom)
            {
                positionCSS = "rz-timeline-bottom";
            }

            return ClassList.Create($"rz-timeline rz-timeline-{(horizontal ? "row" : "column")} {positionCSS} {(Reverse ? "rz-timeline-reverse" : "")}").ToString();
        }
    }
}