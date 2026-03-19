using Bunit;
using Xunit;
using static Radzen.Blazor.Tests.ChartTestHelper;

namespace Radzen.Blazor.Tests
{
    public class BoxPlotSeriesTests
    {
        private static DataItem[] BoxPlotData => new[]
        {
            new DataItem { Category = "Q1", LowerWhisker = 5, LowerQuartile = 10, Median = 15, UpperQuartile = 20, UpperWhisker = 25 },
            new DataItem { Category = "Q2", LowerWhisker = 8, LowerQuartile = 12, Median = 18, UpperQuartile = 22, UpperWhisker = 28 },
        };

        [Fact]
        public void BoxPlotSeries_Renders_BoxPlotClass()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenBoxPlotSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Median))
                    .Add(x => x.LowerWhiskerProperty, nameof(DataItem.LowerWhisker))
                    .Add(x => x.LowerQuartileProperty, nameof(DataItem.LowerQuartile))
                    .Add(x => x.MedianProperty, nameof(DataItem.Median))
                    .Add(x => x.UpperQuartileProperty, nameof(DataItem.UpperQuartile))
                    .Add(x => x.UpperWhiskerProperty, nameof(DataItem.UpperWhisker))
                    .Add(x => x.Data, BoxPlotData)));

            Assert.Contains("rz-box-plot-series", chart.Markup);
        }

        [Fact]
        public void BoxPlotSeries_CustomFillColor_Applied()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenBoxPlotSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Median))
                    .Add(x => x.LowerWhiskerProperty, nameof(DataItem.LowerWhisker))
                    .Add(x => x.LowerQuartileProperty, nameof(DataItem.LowerQuartile))
                    .Add(x => x.MedianProperty, nameof(DataItem.Median))
                    .Add(x => x.UpperQuartileProperty, nameof(DataItem.UpperQuartile))
                    .Add(x => x.UpperWhiskerProperty, nameof(DataItem.UpperWhisker))
                    .Add(x => x.Fill, "#336699")
                    .Add(x => x.Stroke, "#003366")
                    .Add(x => x.Data, BoxPlotData)));

            Assert.Contains("#336699", chart.Markup);
        }

        [Fact]
        public void BoxPlotSeries_Hidden_DoesNotRender()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenBoxPlotSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Median))
                    .Add(x => x.LowerWhiskerProperty, nameof(DataItem.LowerWhisker))
                    .Add(x => x.LowerQuartileProperty, nameof(DataItem.LowerQuartile))
                    .Add(x => x.MedianProperty, nameof(DataItem.Median))
                    .Add(x => x.UpperQuartileProperty, nameof(DataItem.UpperQuartile))
                    .Add(x => x.UpperWhiskerProperty, nameof(DataItem.UpperWhisker))
                    .Add(x => x.Visible, false)
                    .Add(x => x.Data, BoxPlotData)));

            Assert.DoesNotContain("rz-box-plot-series", chart.Markup);
        }
    }
}
