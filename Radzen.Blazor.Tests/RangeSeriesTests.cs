using Bunit;
using Xunit;
using static Radzen.Blazor.Tests.ChartTestHelper;

namespace Radzen.Blazor.Tests
{
    public class RangeSeriesTests
    {
        [Fact]
        public void RangeAreaSeries_Renders_AreaClass()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenRangeAreaSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.MinProperty, nameof(DataItem.Min))
                    .Add(x => x.MaxProperty, nameof(DataItem.Max))
                    .Add(x => x.Data, SampleData)));

            Assert.Contains("rz-range-area-series", chart.Markup);
            Assert.Contains("M ", chart.Markup);
        }

        [Fact]
        public void RangeColumnSeries_Renders_ColumnClass()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenRangeColumnSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.MinProperty, nameof(DataItem.Min))
                    .Add(x => x.MaxProperty, nameof(DataItem.Max))
                    .Add(x => x.Data, SampleData)));

            Assert.Contains("rz-range-column-series", chart.Markup);
        }

        [Fact]
        public void RangeBarSeries_Renders_BarClass()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenRangeBarSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.MinProperty, nameof(DataItem.Min))
                    .Add(x => x.MaxProperty, nameof(DataItem.Max))
                    .Add(x => x.Data, SampleData)));

            Assert.Contains("rz-range-bar-series", chart.Markup);
        }

        [Fact]
        public void RangeAreaSeries_CustomColors_Applied()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenRangeAreaSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.MinProperty, nameof(DataItem.Min))
                    .Add(x => x.MaxProperty, nameof(DataItem.Max))
                    .Add(x => x.Fill, "rgba(100,150,200,0.3)")
                    .Add(x => x.Stroke, "#6496C8")
                    .Add(x => x.Data, SampleData)));

            Assert.Contains("#6496C8", chart.Markup);
            Assert.Contains("rgba(100,150,200,0.3)", chart.Markup);
        }

        [Fact]
        public void RangeColumnSeries_CustomFills_Applied()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenRangeColumnSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.MinProperty, nameof(DataItem.Min))
                    .Add(x => x.MaxProperty, nameof(DataItem.Max))
                    .Add(x => x.Fills, new[] { "#AA0000", "#00AA00", "#0000AA" })
                    .Add(x => x.Data, SampleData)));

            Assert.Contains("#AA0000", chart.Markup);
            Assert.Contains("#00AA00", chart.Markup);
        }
    }
}
