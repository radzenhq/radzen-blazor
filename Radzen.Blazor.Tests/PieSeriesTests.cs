using Bunit;
using Xunit;
using static Radzen.Blazor.Tests.ChartTestHelper;

namespace Radzen.Blazor.Tests
{
    public class PieSeriesTests
    {
        [Fact]
        public void PieSeries_Renders_PieClass()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenPieSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Data, SampleData)));

            Assert.Contains("rz-pie-series", chart.Markup);
        }

        [Fact]
        public void PieSeries_Renders_ArcPerItem()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenPieSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Data, SampleData)));

            // Each data item gets a series-item class
            Assert.Contains("rz-series-item-0", chart.Markup);
            Assert.Contains("rz-series-item-1", chart.Markup);
            Assert.Contains("rz-series-item-2", chart.Markup);
        }

        [Fact]
        public void PieSeries_CustomFills_Applied()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenPieSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Fills, new[] { "#FF0000", "#00FF00", "#0000FF" })
                    .Add(x => x.Data, SampleData)));

            Assert.Contains("#FF0000", chart.Markup);
            Assert.Contains("#00FF00", chart.Markup);
            Assert.Contains("#0000FF", chart.Markup);
        }

        [Fact]
        public void PieSeries_Title_AppearsInLegend()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenPieSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Title, "Sales")
                    .Add(x => x.Data, SampleData)));

            // Pie legend shows category names
            Assert.Contains("A", chart.Markup);
            Assert.Contains("B", chart.Markup);
            Assert.Contains("C", chart.Markup);
        }

        [Fact]
        public void DonutSeries_Renders_PieClass()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenDonutSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Data, SampleData)));

            Assert.Contains("rz-donut-series", chart.Markup);
        }

        [Fact]
        public void DonutSeries_Renders_ArcPerItem()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenDonutSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.InnerRadius, 50)
                    .Add(x => x.Data, SampleData)));

            Assert.Contains("rz-series-item-0", chart.Markup);
            Assert.Contains("rz-series-item-1", chart.Markup);
        }

        [Fact]
        public void PieSeries_Hidden_DoesNotRenderArcs()
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
    }
}
