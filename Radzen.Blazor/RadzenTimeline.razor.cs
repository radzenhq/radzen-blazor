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

        /// <summary>
        /// Gets or sets the TimelineItems content alignment.
        /// </summary>
        /// <value>The TimelineItems content alignment.</value>
        [Parameter]
        public AlignItems AlignItems { get; set; } = AlignItems.Center;

        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            var horizontal = Orientation == Orientation.Horizontal;
            
            var positionCSS = "center";

            if (TimelinePosition == TimelinePosition.Alternate)
            {
                positionCSS = "alternate";
            }
            else if (TimelinePosition == TimelinePosition.Start)
            {
                positionCSS = "start";
            }
            else if (TimelinePosition == TimelinePosition.End)
            {
                positionCSS = "end";
            }
            else if(TimelinePosition == TimelinePosition.Left)
            {
                positionCSS = "left";
            }
            else if (TimelinePosition == TimelinePosition.Right)
            {
                positionCSS = "right";
            }
            else if(TimelinePosition == TimelinePosition.Top)
            {
                positionCSS = "top";
            }
            else if (TimelinePosition == TimelinePosition.Bottom)
            {
                positionCSS = "bottom";
            }

            var alignItemsCSS = "center";

            if (AlignItems == AlignItems.Normal)
            {
                alignItemsCSS = "normal";
            }
            else if (AlignItems == AlignItems.Start)
            {
                alignItemsCSS = "start";
            }
            else if (AlignItems == AlignItems.End)
            {
                alignItemsCSS = "end";
            }
            else if (AlignItems == AlignItems.Stretch)
            {
                alignItemsCSS = "stretch";
            }

            return ClassList.Create($"rz-timeline rz-timeline-{(horizontal ? "row" : "column")} rz-timeline-{positionCSS} {(Reverse ? "rz-timeline-reverse" : "")} rz-timeline-align-items-{alignItemsCSS}").ToString();
        }
    }
}