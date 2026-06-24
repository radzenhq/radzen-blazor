using Bunit;
using Xunit;
using static Radzen.Blazor.Tests.ChartTestHelper;

namespace Radzen.Blazor.Tests
{
    public class ScatterSeriesTests
    {
        [Fact]
        public async System.Threading.Tasks.Task ScatterSeries_Renders_CirclePerDataPoint()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenScatterSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Data, SampleData)));

            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));

            Assert.Contains("rz-scatter-series rz-series-0", chart.Markup);
            // 3 data points = 3 circle markers
            // 3 data circles + 1 legend circle = 4 total
            var circleCount = System.Text.RegularExpressions.Regex.Matches(chart.Markup, @"<circle cx=""[\d.]+""").Count;
            Assert.Equal(4, circleCount);
        }

        [Fact]
        public async System.Threading.Tasks.Task ScatterSeries_CirclePositions_ReflectDataValues()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenScatterSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Data, SampleData)));

            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));

            // A=10 -> cy=236 (bottom), B=20 -> cy=0 (top), C=15 -> cy=118 (mid)
            Assert.Contains(@"cy=""236""", chart.Markup);
            Assert.Contains(@"cy=""0""", chart.Markup);
            Assert.Contains(@"cy=""118""", chart.Markup);
        }

        [Fact]
        public void ScatterSeries_Hidden_NoCirclesRendered()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenScatterSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Visible, false)
                    .Add(x => x.Data, SampleData)));

            Assert.DoesNotContain("rz-scatter-series", chart.Markup);
        }
    }
}
