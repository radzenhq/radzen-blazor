using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    /// <summary>
    /// Crosshair configuration of <see cref="IChartAxis" />. Add inside a
    /// <see cref="RadzenCategoryAxis"/> or <see cref="RadzenValueAxis"/> to draw a crosshair line
    /// for that axis while hovering the chart, similar to Highcharts' <c>xAxis.crosshair</c> /
    /// <c>yAxis.crosshair</c>. The category axis owns the vertical line; the value axis owns the
    /// horizontal line.
    /// </summary>
    public class RadzenAxisCrosshair : RadzenChartComponentBase
    {
        /// <summary>
        /// Specifies whether to display the crosshair line for this axis. Set to <c>false</c> by default.
        /// </summary>
        [Parameter]
        public bool Visible { get; set; } = false;

        /// <summary>
        /// Specifies the crosshair line color. Any valid CSS color. When <c>null</c> the default
        /// <c>var(--rz-chart-crosshair-color)</c> is used.
        /// </summary>
        [Parameter]
        public string? Stroke { get; set; }

        /// <summary>
        /// Specifies the crosshair line width in pixels. Set to <c>1</c> by default.
        /// </summary>
        [Parameter]
        public double StrokeWidth { get; set; } = 1;

        /// <summary>
        /// Specifies the crosshair line style. Defaults to <see cref="LineType.Dashed"/>.
        /// </summary>
        [Parameter]
        public LineType LineType { get; set; } = LineType.Dashed;

        /// <summary>
        /// Specifies whether the crosshair snaps to the nearest data point on this axis. Set to
        /// <c>true</c> by default. When <c>false</c> the line follows the cursor position exactly.
        /// Only meaningful for the category axis line; ignored on the value axis.
        /// </summary>
        [Parameter]
        public bool Snap { get; set; } = true;

        /// <summary>
        /// Specifies whether to display a small label at the axis showing the formatted axis value
        /// where the crosshair crosses it. The label uses the parent axis's <c>Formatter</c> /
        /// <c>FormatString</c>. Set to <c>false</c> by default.
        /// </summary>
        [Parameter]
        public bool Label { get; set; } = false;

        /// <summary>
        /// The axis which this configuration applies to.
        /// </summary>
        [CascadingParameter]
        public IChartAxis? ChartAxis
        {
            set
            {
                if (value != null)
                    value.Crosshair = this;
            }
        }

        /// <inheritdoc />
        override protected bool ShouldRefreshChart(ParameterView parameters)
        {
            return DidParameterChange(parameters, nameof(Visible), Visible);
        }
    }
}
