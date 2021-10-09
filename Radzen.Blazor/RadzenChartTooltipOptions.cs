using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    /// <summary>
    /// Class RadzenChartTooltipOptions.
    /// Implements the <see cref="Radzen.Blazor.RadzenChartComponentBase" />
    /// </summary>
    /// <seealso cref="Radzen.Blazor.RadzenChartComponentBase" />
    public partial class RadzenChartTooltipOptions : RadzenChartComponentBase
    {
        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="RadzenChartTooltipOptions"/> is visible.
        /// </summary>
        /// <value><c>true</c> if visible; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool Visible { get; set; } = true;

        /// <summary>
        /// Gets or sets the style.
        /// </summary>
        /// <value>The style.</value>
        [Parameter]
        public string Style { get; set; }

        /// <summary>
        /// Initializes this instance.
        /// </summary>
        protected override void Initialize()
        {
            Chart.Tooltip = this;
        }

        /// <summary>
        /// Shoulds the refresh chart.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        protected override bool ShouldRefreshChart(ParameterView parameters)
        {
            return parameters.DidParameterChange(nameof(Style), Style);
        }
    }
}
