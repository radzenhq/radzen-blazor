using Bunit;
using Xunit;
using static Radzen.Blazor.Tests.ChartTestHelper;

namespace Radzen.Blazor.Tests
{
    public class PieSeriesTests
    {
        [Fact]
        public void PieSeries_Renders_ArcPathPerItem()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenPieSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Fills, new[] { "#FF0000", "#00FF00", "#0000FF" })
                    .Add(x => x.Data, SampleData)));

            Assert.Contains("rz-pie-series rz-series-0", chart.Markup);
            Assert.Contains("rz-series-item-0", chart.Markup);
            Assert.Contains("rz-series-item-1", chart.Markup);
            Assert.Contains("rz-series-item-2", chart.Markup);
        }

        [Fact]
        public void PieSeries_CustomFills_AppliedToArcPaths()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenPieSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Fills, new[] { "#FF0000", "#00FF00", "#0000FF" })
                    .Add(x => x.Data, SampleData)));

            Assert.Contains("fill: #FF0000", chart.Markup);
            Assert.Contains("fill: #00FF00", chart.Markup);
            Assert.Contains("fill: #0000FF", chart.Markup);
        }

        [Fact]
        public async System.Threading.Tasks.Task PieSeries_ArcPaths_ContainSvgArcCommands()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenPieSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Data, SampleData)));

            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));

            // Pie arcs use SVG A (arc) command with radius 118
            Assert.Contains("A 118 118", chart.Markup);
        }

        [Fact]
        public void PieSeries_Legend_ShowsCategoryNames()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenPieSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Data, SampleData)));

            // Pie legend shows category names, not series title
            Assert.Contains(">A</span>", chart.Markup);
            Assert.Contains(">B</span>", chart.Markup);
            Assert.Contains(">C</span>", chart.Markup);
        }

        [Fact]
        public void PieSeries_Hidden_NoArcPathsRendered()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenPieSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Visible, false)
                    .Add(x => x.Data, SampleData)));

            Assert.DoesNotContain("rz-pie-series", chart.Markup);
        }

        [Fact]
        public async System.Threading.Tasks.Task DonutSeries_Renders_DonutClass()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenDonutSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.InnerRadius, 50)
                    .Add(x => x.Data, SampleData)));

            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));

            Assert.Contains("rz-donut-series", chart.Markup);
            Assert.Contains("rz-series-item-0", chart.Markup);
            // Donut arcs have both outer and inner radius in the path
            Assert.Contains("A 118 118", chart.Markup); // outer
        }
    }
}
