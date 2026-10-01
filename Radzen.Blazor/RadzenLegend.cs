using Microsoft.AspNetCore.Components;
using System;
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
        /// <value>The position. Default is <see cref="LegendPosition.End"/>, which renders on the right
        /// in left-to-right mode and automatically flips to the left in right-to-left mode.</value>
        [Parameter]
        public LegendPosition Position { get; set; } = LegendPosition.End;

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="RadzenLegend"/> is visible.
        /// </summary>
        /// <value><c>true</c> if visible; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool Visible { get; set; } = true;

        /// <summary>
        /// Gets or sets the template which renders the content of every legend item instead of its text.
        /// The marker and the click behavior of the legend item are preserved. The chart measures the rendered legend and reserves the space it needs.
        /// </summary>
        /// <example>
        /// <code>
        /// &lt;RadzenLegend&gt;
        ///     &lt;ItemTemplate Context="item"&gt;
        ///         &lt;div&gt;@item.Text&lt;/div&gt;
        ///         &lt;em&gt;@(((DataItem)item.Data).Revenue.ToString("C0"))&lt;/em&gt;
        ///     &lt;/ItemTemplate&gt;
        /// &lt;/RadzenLegend&gt;
        /// </code>
        /// </example>
        /// <value>The item template.</value>
        [Parameter]
        public RenderFragment<LegendItemContext>? ItemTemplate { get; set; }

        internal double Measure(RadzenChart chart)
        {
            if (!Visible)
            {
                return 0;
            }

            var vertical = Position == LegendPosition.Right || Position == LegendPosition.Left
                || Position == LegendPosition.Start || Position == LegendPosition.End;

            if (ItemTemplate != null && chart.MeasuredLegendSize is { } measured && measured.Vertical == vertical)
            {
                return Math.Max(0, measured.Size - 16);
            }

            double length = 0;

            var size = 16 * 0.875;

            if (vertical)
            {
                if (chart.Series.Any())
                {
                    length = chart.Series.Select(series => series.MeasureLegend()).Max();
                    size = length + 8 + 2 * chart.Series.Select(series => series.MarkerSize).Max();
                }
            }
            else
            {
                var available = chart.MeasuredWidth ?? 0;

                if (available > 0)
                {
                    var rows = 1;
                    double x = 0;

                    foreach (var series in chart.Series)
                    {
                        if (!series.ShowInLegend)
                        {
                            continue;
                        }

                        var marker = 2 * series.MarkerSize;

                        foreach (var text in series.MeasureLegendItems())
                        {
                            var itemWidth = text + 8 + marker;

                            if (x > 0 && x + itemWidth > available)
                            {
                                rows++;
                                x = 0;
                            }

                            x += itemWidth;
                        }
                    }

                    size += (rows - 1) * (size + 8);
                }
            }

            return size;
        }

        /// <summary>
        /// Initializes this instance.
        /// </summary>
        protected override void Initialize()
        {
            if (Chart != null)
            {
                Chart.Legend = this;
            }
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