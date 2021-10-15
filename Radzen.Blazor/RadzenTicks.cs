using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    /// <summary>
    /// Tick configuration of <see cref="IChartAxis" />. 
    /// </summary>
    public class RadzenTicks : ComponentBase
    {
        /// <summary>
        /// Specifies the color of the ticks lines.
        /// </summary>
        [Parameter]
        public string Stroke { get; set; }

        /// <summary>
        /// Specifies the width of the tick lines. Set to <c>1</c> by default.
        /// </summary>
        [Parameter]
        public double StrokeWidth { get; set; } = 1;

        /// <summary>
        /// Specifies the type of line used to render the ticks.
        /// </summary>
        [Parameter]
        public LineType LineType { get; set; }

        /// <summary>
        /// The axis which this configuration applies to.
        /// </summary>
        [CascadingParameter]
        public AxisBase ChartAxis
        {
            set
            {
                value.Ticks = this;
            }
        }

        /// <summary>
        /// Gets or sets the template.
        /// </summary>
        /// <value>The template.</value>
        [Parameter]
        public RenderFragment<TickTemplateContext> Template { get; set; }
    }
}