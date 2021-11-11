using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    /// <summary>
    /// Grid line configuration of <see cref="IChartAxis" />.
    /// </summary>
    public class RadzenGridLines : RadzenChartComponentBase
    {
        /// <summary>
        /// Specifies the color of the grid lines.
        /// </summary>
        [Parameter]
        public string Stroke { get; set; }

        /// <summary>
        /// Specifies the pixel width of the grid lines. Set to <c>1</c> by default.
        /// </summary>
        [Parameter]
        public double StrokeWidth { get; set; } = 1;

        /// <summary>
        /// Specifies the type of line used to render the grid lines.
        /// </summary>
        [Parameter]
        public LineType LineType { get; set; }

        /// <summary>
        /// Specifies whether to display grid lines or not. Set to <c>false</c> by default.
        /// </summary>
        [Parameter]
        public bool Visible { get; set; } = false;

        /// <summary>
        /// The axis which this configuration applies to.
        /// </summary>
        [CascadingParameter]
        public IChartAxis ChartAxis
        {
            set
            {
                value.GridLines = this;
            }
        }

        /// <inheritdoc />
        override protected bool ShouldRefreshChart(ParameterView parameters)
        {
            return DidParameterChange(parameters, nameof(Visible), Visible);
        }
    }
}