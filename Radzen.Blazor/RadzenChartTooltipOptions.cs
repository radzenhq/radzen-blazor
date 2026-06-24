using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
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
        /// Enable split tooltips — one small tooltip box per series, each anchored near its own data point at the snapped category X. Off by default.
        /// </summary>
        [Parameter]
        public bool Split { get; set; }

        /// <summary>
        /// Specifies when the tooltip is displayed. <see cref="ChartTooltipTrigger.Point" /> requires the cursor to be near a data point;
        /// <see cref="ChartTooltipTrigger.Axis" /> follows the nearest category anywhere inside the plot area.
        /// The default <see cref="ChartTooltipTrigger.Auto" /> uses Axis when the category crosshair is enabled or the chart is in a sync group.
        /// </summary>
        /// <value>The tooltip trigger. Default is <see cref="ChartTooltipTrigger.Auto" />.</value>
        [Parameter]
        public ChartTooltipTrigger Trigger { get; set; } = ChartTooltipTrigger.Auto;

        /// <summary>
        /// Enable or disable highlighting of the data point the tooltip refers to.
        /// When enabled an enlarged dot with a soft halo is rendered at the active data point and glides between points as the cursor moves.
        /// With <see cref="Shared"/> tooltips a dot is rendered for every series at the snapped category. On by default.
        /// </summary>
        /// <value><c>true</c> to highlight the active data point; otherwise, <c>false</c>. Default is <c>true</c>.</value>
        [Parameter]
        public bool HighlightDataPoint { get; set; } = true;

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
                || parameters.DidParameterChange(nameof(Trigger), Trigger)
                || parameters.DidParameterChange(nameof(HighlightDataPoint), HighlightDataPoint);
        }
    }
}
