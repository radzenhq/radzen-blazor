using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    /// <summary>
    /// Class RadzenBarOptions.
    /// Implements the <see cref="Radzen.Blazor.RadzenChartComponentBase" />
    /// </summary>
    /// <seealso cref="Radzen.Blazor.RadzenChartComponentBase" />
    public partial class RadzenBarOptions : RadzenChartComponentBase
    {
        /// <summary>
        /// Gets or sets the radius.
        /// </summary>
        /// <value>The radius.</value>
        [Parameter]
        public double Radius { get; set; }

        /// <summary>
        /// Gets or sets the margin.
        /// </summary>
        /// <value>The margin.</value>
        [Parameter]
        public double Margin { get; set; } = 10;

        /// <summary>
        /// Initializes this instance.
        /// </summary>
        protected override void Initialize()
        {
            Chart.BarOptions = this;
        }

        /// <summary>
        /// Shoulds the refresh chart.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        protected override bool ShouldRefreshChart(ParameterView parameters)
        {
            return DidParameterChange(parameters, nameof(Radius), Radius) || DidParameterChange(parameters, nameof(Margin), Margin);
        }
    }
}