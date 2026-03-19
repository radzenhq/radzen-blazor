using Bunit;
using Xunit;
using static Radzen.Blazor.Tests.ChartTestHelper;

namespace Radzen.Blazor.Tests
{
    public class AreaSeriesTests
    {
        [Fact]
        public void AreaSeries_Renders_FillAndStrokePaths()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenAreaSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Fill, "rgba(255,0,0,0.3)")
                    .Add(x => x.Stroke, "#FF0000")
                    .Add(x => x.Data, SampleData)));

            Assert.Contains("rz-area-series rz-series-0", chart.Markup);
            // Area has both a filled path (area) and a stroke-only path (line on top)
            Assert.Contains("stroke: #FF0000", chart.Markup);
            Assert.Contains("fill: none", chart.Markup); // the line path
            Assert.Contains("rgba(255,0,0,0.3)", chart.Markup); // the area fill
        }

        [Fact]
        public void AreaSeries_Smooth_UsesCubicBezierPath()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenAreaSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Smooth, true)
                    .Add(x => x.Data, SampleData)));

            var series = chart.FindComponent<RadzenAreaSeries<DataItem>>();
            Assert.Equal(Interpolation.Spline, series.Instance.Interpolation);
            Assert.Contains(" C ", chart.Markup);
        }

        [Fact]
        public void AreaSeries_Hidden_NoSeriesGroupRendered()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenAreaSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Visible, false)
                    .Add(x => x.Data, SampleData)));

            Assert.DoesNotContain("rz-area-series", chart.Markup);
        }

        [Fact]
        public void AreaSeries_Legend_ShowsTitle()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenAreaSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Title, "Temperature")
                    .Add(x => x.Data, SampleData)));

            Assert.Contains(">Temperature</span>", chart.Markup);
        }
    }
}
