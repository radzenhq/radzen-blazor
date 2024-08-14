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
        public string Style { get; set; }

        /// <summary>
        /// Enable or disable shared tooltips (one tooltip displaying data for all values for the same category). By default set to false (a separate tooltip is shown for each point in the category).
        /// </summary>
        [Parameter]
        public bool Shared { get; set; }

        /// <inheritdoc />
        protected override void Initialize()
        {
            Chart.Tooltip = this;
        }

        /// <inheritdoc />
        protected override bool ShouldRefreshChart(ParameterView parameters)
        {
            return parameters.DidParameterChange(nameof(Style), Style);
        }
    }
}
