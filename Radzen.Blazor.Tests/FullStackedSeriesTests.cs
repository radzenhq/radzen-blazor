using Bunit;
using Xunit;
using static Radzen.Blazor.Tests.ChartTestHelper;

namespace Radzen.Blazor.Tests
{
    public class FullStackedSeriesTests
    {
        [Fact]
        public void FullStackedColumnSeries_Renders_ColumnClass()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenFullStackedColumnSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Data, SampleData))
                .AddChildContent<RadzenFullStackedColumnSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value2))
                    .Add(x => x.Data, SampleData)));

            Assert.Contains("rz-full-stacked-column-series", chart.Markup);
            Assert.Contains("rz-series-0", chart.Markup);
            Assert.Contains("rz-series-1", chart.Markup);
        }

        [Fact]
        public void FullStackedBarSeries_Renders_BarClass()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenFullStackedBarSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Data, SampleData))
                .AddChildContent<RadzenFullStackedBarSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value2))
                    .Add(x => x.Data, SampleData)));

            Assert.Contains("rz-full-stacked-bar-series", chart.Markup);
            Assert.Contains("rz-series-0", chart.Markup);
            Assert.Contains("rz-series-1", chart.Markup);
        }

        [Fact]
        public void FullStackedAreaSeries_Renders_AreaClass()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenFullStackedAreaSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Data, SampleData))
                .AddChildContent<RadzenFullStackedAreaSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value2))
                    .Add(x => x.Data, SampleData)));

            Assert.Contains("rz-full-stacked-area-series", chart.Markup);
            Assert.Contains("rz-series-0", chart.Markup);
            Assert.Contains("rz-series-1", chart.Markup);
        }

        [Fact]
        public void FullStackedLineSeries_Renders_LineClass()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenFullStackedLineSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Data, SampleData))
                .AddChildContent<RadzenFullStackedLineSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value2))
                    .Add(x => x.Data, SampleData)));

            Assert.Contains("rz-full-stacked-line-series", chart.Markup);
            Assert.Contains("rz-series-0", chart.Markup);
            Assert.Contains("rz-series-1", chart.Markup);
        }

        [Fact]
        public void StackedLineSeries_Renders_LineClass()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenStackedLineSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Data, SampleData))
                .AddChildContent<RadzenStackedLineSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value2))
                    .Add(x => x.Data, SampleData)));

            Assert.Contains("rz-stacked-line-series", chart.Markup);
            Assert.Contains("rz-series-0", chart.Markup);
            Assert.Contains("rz-series-1", chart.Markup);
        }
    }
}
