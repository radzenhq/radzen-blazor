using Bunit;
using Xunit;
using static Radzen.Blazor.Tests.ChartTestHelper;

namespace Radzen.Blazor.Tests
{
    public class FunnelSeriesTests
    {
        [Fact]
        public void FunnelSeries_Renders_FunnelClass()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenFunnelSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Data, SampleData)));

            Assert.Contains("rz-funnel-series", chart.Markup);
        }

        [Fact]
        public void FunnelSeries_Renders_SegmentPerItem()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenFunnelSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Data, SampleData)));

            Assert.Contains("rz-series-item-0", chart.Markup);
            Assert.Contains("rz-series-item-1", chart.Markup);
            Assert.Contains("rz-series-item-2", chart.Markup);
        }

        [Fact]
        public void FunnelSeries_ShowLabels_RendersCategoryNames()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenFunnelSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Data, SampleData)));

            Assert.Contains("A", chart.Markup);
            Assert.Contains("B", chart.Markup);
            Assert.Contains("C", chart.Markup);
        }

        [Fact]
        public void FunnelSeries_CustomFills_Applied()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenFunnelSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Fills, new[] { "#FF0000", "#00FF00", "#0000FF" })
                    .Add(x => x.Data, SampleData)));

            Assert.Contains("#FF0000", chart.Markup);
            Assert.Contains("#00FF00", chart.Markup);
            Assert.Contains("#0000FF", chart.Markup);
        }

        [Fact]
        public void FunnelSeries_Hidden_DoesNotRender()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenFunnelSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Visible, false)
                    .Add(x => x.Data, SampleData)));

            Assert.DoesNotContain("rz-funnel-series", chart.Markup);
        }
    }
}
