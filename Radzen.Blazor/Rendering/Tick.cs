using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor.Rendering
{
    /// <summary> Base component for all chart ticks. </summary>
    public abstract class Tick : ComponentBase
    {
        /// <summary> Gets or sets the X coordinate. </summary>
        [Parameter]
        public double X { get; set; }

        /// <summary> Gets or sets the Y coordinate. </summary>
        [Parameter]
        public double Y { get; set; }

        /// <summary> Gets or sets the child content. </summary>
        [Parameter]
        public RenderFragment ChildContent { get; set; }

        /// <summary> Gets or sets the stroke (line color) of the tick. </summary>
        [Parameter]
        public string Stroke { get; set; }

        /// <summary> Gets or sets the pixel width of the tick. </summary>
        [Parameter]
        public double StrokeWidth { get ; set; }

        /// <summary> Gets or sets the type of the line used to display the tick. </summary>
        [Parameter]
        public LineType LineType { get; set; }

        /// <summary> Gets or sets the text of the tick. </summary>
        [Parameter]
        public string Text { get; set; }
    }
}
