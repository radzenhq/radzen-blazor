using Bunit;
using Xunit;
using static Radzen.Blazor.Tests.ChartTestHelper;

namespace Radzen.Blazor.Tests
{
    public class ColumnSeriesTests
    {
        [Fact]
        public void ColumnSeries_Renders_ThreeColumnPaths()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenColumnSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Fill, "#336699")
                    .Add(x => x.Data, SampleData)));

            Assert.Contains("rz-column-series rz-series-0", chart.Markup);
            // 3 data items = 3 path elements with fill color
            var fillCount = System.Text.RegularExpressions.Regex.Matches(chart.Markup, "fill: #336699").Count;
            Assert.Equal(3, fillCount);
        }

        [Fact]
        public async System.Threading.Tasks.Task ColumnSeries_ColumnHeights_ReflectDataValues()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenColumnSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Fill, "#336699")
                    .Add(x => x.Data, SampleData)));

            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));

            var markup = chart.Markup;
            // B=20 (max) -> column top at y=0, ends at y=236 (baseline)
            // C=15 (mid) -> column top at y=118, ends at y=236
            // Column paths end with "L <x> 236 Z" (returning to baseline)
            Assert.Matches(@"L\s+[\d.]+\s+236\s+Z", markup);
            // B column reaches y=0 (top of chart area)
            Assert.Matches(@"M\s+[\d.]+\s+0\s+A", markup);
        }

        [Fact]
        public void ColumnSeries_IndividualFills_EachColumnDifferentColor()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenColumnSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Fills, new[] { "#FF0000", "#00FF00", "#0000FF" })
                    .Add(x => x.Data, SampleData)));

            Assert.Contains("fill: #FF0000", chart.Markup);
            Assert.Contains("fill: #00FF00", chart.Markup);
            Assert.Contains("fill: #0000FF", chart.Markup);
        }

        [Fact]
        public void ColumnSeries_MultipleSeries_LegendShowsTitles()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenColumnSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Title, "2023")
                    .Add(x => x.Data, SampleData))
                .AddChildContent<RadzenColumnSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value2))
                    .Add(x => x.Title, "2024")
                    .Add(x => x.Data, SampleData)));

            Assert.Contains("rz-series-0", chart.Markup);
            Assert.Contains("rz-series-1", chart.Markup);
            Assert.Contains(">2023</span>", chart.Markup);
            Assert.Contains(">2024</span>", chart.Markup);
        }

        [Fact]
        public void ColumnSeries_Hidden_NoSeriesGroupRendered()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenColumnSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Visible, false)
                    .Add(x => x.Data, SampleData)));

            Assert.DoesNotContain("rz-column-series", chart.Markup);
        }
    }
}
