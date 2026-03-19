using Bunit;
using Xunit;
using static Radzen.Blazor.Tests.ChartTestHelper;

namespace Radzen.Blazor.Tests
{
    public class StackedSeriesTests
    {
        [Fact]
        public void StackedAreaSeries_Renders_AreaClass()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenStackedAreaSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Data, SampleData))
                .AddChildContent<RadzenStackedAreaSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value2))
                    .Add(x => x.Data, SampleData)));

            Assert.Contains("rz-area-series", chart.Markup);
            Assert.Contains("rz-series-0", chart.Markup);
            Assert.Contains("rz-series-1", chart.Markup);
        }

        [Fact]
        public void StackedBarSeries_Renders_BarClass()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenStackedBarSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Data, SampleData))
                .AddChildContent<RadzenStackedBarSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value2))
                    .Add(x => x.Data, SampleData)));

            Assert.Contains("rz-bar-series", chart.Markup);
            Assert.Contains("rz-series-0", chart.Markup);
            Assert.Contains("rz-series-1", chart.Markup);
        }

        [Fact]
        public void StackedColumnSeries_Renders_ColumnClass()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenStackedColumnSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Data, SampleData))
                .AddChildContent<RadzenStackedColumnSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value2))
                    .Add(x => x.Data, SampleData)));

            Assert.Contains("rz-column-series rz-series-0", chart.Markup);
            Assert.Contains("rz-column-series rz-series-1", chart.Markup);
        }

        [Fact]
        public void StackedColumnSeries_CustomFills_Applied()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenStackedColumnSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Fill, "#336699")
                    .Add(x => x.Data, SampleData))
                .AddChildContent<RadzenStackedColumnSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value2))
                    .Add(x => x.Fill, "#996633")
                    .Add(x => x.Data, SampleData)));

            Assert.Contains("#336699", chart.Markup);
            Assert.Contains("#996633", chart.Markup);
        }

        [Fact]
        public void StackedColumnSeries_Titles_InLegend()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenStackedColumnSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Title, "2023")
                    .Add(x => x.Data, SampleData))
                .AddChildContent<RadzenStackedColumnSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value2))
                    .Add(x => x.Title, "2024")
                    .Add(x => x.Data, SampleData)));

            Assert.Contains("2023", chart.Markup);
            Assert.Contains("2024", chart.Markup);
        }
    }
}
