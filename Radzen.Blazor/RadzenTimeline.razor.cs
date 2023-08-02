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
        public LinePosition LinePosition { get; set; } = LinePosition.Center;

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

            if (LinePosition == LinePosition.Alternate)
            {
                positionCSS = "alternate";
            }
            else if (LinePosition == LinePosition.Start)
            {
                positionCSS = "start";
            }
            else if (LinePosition == LinePosition.End)
            {
                positionCSS = "end";
            }
            else if(LinePosition == LinePosition.Left)
            {
                positionCSS = "left";
            }
            else if (LinePosition == LinePosition.Right)
            {
                positionCSS = "right";
            }
            else if(LinePosition == LinePosition.Top)
            {
                positionCSS = "top";
            }
            else if (LinePosition == LinePosition.Bottom)
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