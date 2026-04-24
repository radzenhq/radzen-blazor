using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    /// <summary>
    /// Specifies which crosshair lines are drawn while hovering the chart.
    /// </summary>
    public enum CrosshairMode
    {
        /// <summary>No crosshair (default).</summary>
        None,
        /// <summary>Draw only a vertical line at the snapped category X position.</summary>
        X,
        /// <summary>Draw only a horizontal line at the cursor Y position.</summary>
        Y,
        /// <summary>Draw both vertical and horizontal crosshair lines.</summary>
        Both
    }

    /// <summary>
    /// Contains <see cref="RadzenChart" /> tooltip configuration.
    /// </summary>
    public partial class RadzenChartTooltipOptions : RadzenChartComponentBase
    {
        /// <summary>
        /// Gets or sets a value indicating whether to show tooltips. By default RadzenChart displays tooltips.
        /// </summary>
        /// <value><c>true</c> to display tooltips; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool Visible { get; set; } = true;

        /// <summary>
        /// Gets or sets the CSS style of the tooltip.
        /// </summary>
        /// <value>The style.</value>
        [Parameter]
        public string? Style { get; set; }

        /// <summary>
        /// Enable or disable shared tooltips (one tooltip displaying data for all values for the same category). By default set to false (a separate tooltip is shown for each point in the category).
        /// </summary>
        [Parameter]
        public bool Shared { get; set; }

        /// <summary>
        /// Enable split tooltips — one small tooltip box per series, each anchored near its own data point at the snapped category X (similar to Highcharts' split tooltip). Off by default.
        /// </summary>
        [Parameter]
        public bool Split { get; set; }

        /// <summary>
        /// Which crosshair lines to draw while hovering the chart. Defaults to <see cref="CrosshairMode.None"/> (no crosshair).
        /// Set to <see cref="CrosshairMode.X"/>, <see cref="CrosshairMode.Y"/> or <see cref="CrosshairMode.Both"/> to enable.
        /// </summary>
        [Parameter]
        public CrosshairMode CrosshairMode { get; set; } = CrosshairMode.None;

        /// <summary>
        /// Crosshair line color. Any valid CSS color. When <c>null</c> the default <c>var(--rz-chart-crosshair-color)</c> is used.
        /// </summary>
        [Parameter]
        public string? CrosshairColor { get; set; }

        /// <summary>
        /// Crosshair line style. Defaults to <see cref="LineType.Dashed"/>.
        /// </summary>
        [Parameter]
        public LineType CrosshairLineType { get; set; } = LineType.Dashed;

        /// <summary>
        /// Crosshair line width in pixels. Defaults to <c>1</c>.
        /// </summary>
        [Parameter]
        public double CrosshairStrokeWidth { get; set; } = 1;

        /// <inheritdoc />
        protected override void Initialize()
        {
            if (Chart != null)
            {
                Chart.Tooltip = this;
            }
        }

        /// <inheritdoc />
        protected override bool ShouldRefreshChart(ParameterView parameters)
        {
            return parameters.DidParameterChange(nameof(Style), Style)
                || parameters.DidParameterChange(nameof(Shared), Shared)
                || parameters.DidParameterChange(nameof(Split), Split)
                || parameters.DidParameterChange(nameof(CrosshairMode), CrosshairMode)
                || parameters.DidParameterChange(nameof(CrosshairColor), CrosshairColor)
                || parameters.DidParameterChange(nameof(CrosshairLineType), CrosshairLineType)
                || parameters.DidParameterChange(nameof(CrosshairStrokeWidth), CrosshairStrokeWidth);
        }
    }
}
