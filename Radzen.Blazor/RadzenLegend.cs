using Microsoft.AspNetCore.Components;
using System.Linq;

namespace Radzen.Blazor
{
    /// <summary>
    /// Class RadzenLegend.
    /// </summary>
    public class RadzenLegend : RadzenChartComponentBase
    {
        /// <summary>
        /// Gets or sets the position.
        /// </summary>
        /// <value>The position.</value>
        [Parameter]
        public LegendPosition Position { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="RadzenLegend"/> is visible.
        /// </summary>
        /// <value><c>true</c> if visible; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool Visible { get; set; } = true;

        internal double Measure(RadzenChart chart)
        {
            if (!Visible)
            {
                return 0;
            }

            double length = 0;

            var size = 16 * 0.875;

            if (Position == LegendPosition.Right || Position == LegendPosition.Left)
            {
                if (chart.Series.Any())
                {
                    length = chart.Series.Select(series => series.MeasureLegend()).Max();
                    size = length + 8 + 2 * chart.Series.Select(series => series.MarkerSize).Max();
                }
            }

            return size;
        }

        /// <summary>
        /// Initializes this instance.
        /// </summary>
        protected override void Initialize()
        {
            Chart.Legend = this;
        }

        /// <summary>
        /// Shoulds the refresh chart.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        protected override bool ShouldRefreshChart(ParameterView parameters)
        {
            return DidParameterChange(parameters, nameof(Position), Position) || DidParameterChange(parameters, nameof(Visible), Visible);
        }
    }
}