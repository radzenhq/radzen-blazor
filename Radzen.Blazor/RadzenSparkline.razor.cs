using System;
using System.Linq;

namespace Radzen.Blazor
{
    /// <summary>
    /// A sparkline is a small chart that provides a simple way to visualize trends in data.
    /// </summary>
    public partial class RadzenSparkline : RadzenChart
    {
        /// <summary>
        /// Instantiates a new instance of <see cref="RadzenSparkline" />.
        /// </summary>
        public RadzenSparkline()
        {
            CategoryAxis = new RadzenCategoryAxis { Visible = false };
            ValueAxis = new RadzenValueAxis { Visible = false };
            Legend = new RadzenLegend { Visible = false };
            TooltipTolerance = 5;
        }

        /// <summary>
        /// Updates the scales based on the configuration.
        /// </summary>
        /// <returns>True if the chart should refresh; otherwise false;</returns>
        protected override bool UpdateScales()
        {
            var valueScale = ValueScale;
            var categoryScale = CategoryScale;

            CategoryScale = new LinearScale { Output = CategoryScale.Output };
            ValueScale = new LinearScale { Output = ValueScale.Output };

            var visibleSeries = Series.Where(series => series.Visible).ToList();
            var invisibleSeries = Series.Where(series => series.Visible == false).ToList();

            if (!visibleSeries.Any() && invisibleSeries.Any())
            {
                visibleSeries.Add(invisibleSeries.Last());
            }

            foreach (var series in visibleSeries)
            {
                CategoryScale = series.TransformCategoryScale(CategoryScale);
                ValueScale = series.TransformValueScale(ValueScale);
            }

            AxisBase xAxis = CategoryAxis;
            AxisBase yAxis = ValueAxis;

            if (ShouldInvertAxes())
            {
                xAxis = ValueAxis;
                yAxis = CategoryAxis;
            }
            else
            {
                CategoryScale.Padding = CategoryAxis.Padding;
            }

            CategoryScale.Resize(xAxis.Min, xAxis.Max);

            if (xAxis.Step != null)
            {
                CategoryScale.Step = xAxis.Step;
                CategoryScale.Round = false;
            }

            ValueScale.Resize(yAxis.Min, yAxis.Max);

            if (yAxis.Step != null)
            {
                ValueScale.Step = yAxis.Step;
                ValueScale.Round = false;
            }

            var legendSize = Legend.Measure(this);
            var valueAxisSize = ValueAxis.Measure(this);
            var categoryAxisSize = CategoryAxis.Measure(this);

            if (!ShouldRenderAxes())
            {
                valueAxisSize = categoryAxisSize = 0;
            }

            if (ValueAxis.Visible)
            {
                MarginLeft = valueAxisSize;
            }

            if (Legend.Visible)
            {
                if (Legend.Position == LegendPosition.Right || Legend.Position == LegendPosition.Left)
                {
                    if (Legend.Position == LegendPosition.Right)
                    {
                        MarginRight = legendSize + 16;
                    }
                    else
                    {
                        MarginLeft = legendSize + 16 + valueAxisSize;
                    }
                }
                else if (Legend.Position == LegendPosition.Top || Legend.Position == LegendPosition.Bottom)
                {
                    if (Legend.Position == LegendPosition.Top)
                    {
                        MarginTop = legendSize + 16;
                    }
                    else
                    {
                        MarginBottom = legendSize + 16 + categoryAxisSize;
                    }
                }
            }

            CategoryScale.Output = new ScaleRange { Start = MarginLeft, End = Width.Value - MarginRight };
            ValueScale.Output = new ScaleRange { Start = Height.Value - MarginBottom, End = MarginTop };

            ValueScale.Fit(ValueAxis.TickDistance);
            CategoryScale.Fit(CategoryAxis.TickDistance);

            var stateHasChanged = !ValueScale.IsEqualTo(valueScale);

            if (!CategoryScale.IsEqualTo(categoryScale))
            {
                stateHasChanged = true;
            }

            return stateHasChanged;
        }
    }
}