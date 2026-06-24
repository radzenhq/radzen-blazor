using Bunit;
using Xunit;
using static Radzen.Blazor.Tests.ChartTestHelper;

namespace Radzen.Blazor.Tests
{
    public class PyramidSeriesTests
    {
        [Fact]
        public void PyramidSeries_Renders_PyramidClass()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenPyramidSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Data, SampleData)));

            Assert.Contains("rz-pyramid-series", chart.Markup);
        }

        [Fact]
        public void PyramidSeries_Renders_SegmentPerItem()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenPyramidSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Data, SampleData)));

            Assert.Contains("rz-series-item-0", chart.Markup);
            Assert.Contains("rz-series-item-1", chart.Markup);
            Assert.Contains("rz-series-item-2", chart.Markup);
        }

        [Fact]
        public void PyramidSeries_ShowLabels_RendersCategoryNames()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenPyramidSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Data, SampleData)));

            Assert.Contains("A", chart.Markup);
            Assert.Contains("B", chart.Markup);
            Assert.Contains("C", chart.Markup);
        }

        [Fact]
        public void PyramidSeries_Hidden_DoesNotRender()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenPyramidSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Visible, false)
                    .Add(x => x.Data, SampleData)));

            Assert.DoesNotContain("rz-pyramid-series", chart.Markup);
        }
    }
}
