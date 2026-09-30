using Microsoft.AspNetCore.Components;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    /// <summary>
    /// Tick configuration of <see cref="IChartAxis" />. 
    /// </summary>
    public class RadzenTicks : ComponentBase
    {
        private AxisBase? axis;

        /// <summary>
        /// Specifies the color of the ticks lines.
        /// </summary>
        [Parameter]
        public string? Stroke { get; set; }

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
        public AxisBase? ChartAxis
        {
            set
            {
                axis = value;

                if (value != null)
                {
                    value.Ticks = this;
                }
            }
        }

        /// <summary>
        /// Gets or sets the template.
        /// </summary>
        /// <value>The template.</value>
        [Parameter]
        public RenderFragment<TickTemplateContext>? Template { get; set; }

        /// <inheritdoc />
        public override async Task SetParametersAsync(ParameterView parameters)
        {
            var hadTemplate = Template != null;

            await base.SetParametersAsync(parameters);

            if (hadTemplate != (Template != null) && axis?.Chart != null)
            {
                await axis.Chart.Refresh();
            }
        }
    }
}