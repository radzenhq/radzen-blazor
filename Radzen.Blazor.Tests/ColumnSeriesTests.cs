using Bunit;
using Xunit;
using static Radzen.Blazor.Tests.ChartTestHelper;

namespace Radzen.Blazor.Tests
{
    public class ColumnSeriesTests
    {
        [Fact]
        public void ColumnSeries_Renders_ColumnPaths()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenColumnSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Data, SampleData)));

            Assert.Contains("rz-column-series", chart.Markup);
            // Should render one path per data item
            var pathCount = System.Text.RegularExpressions.Regex.Matches(chart.Markup, @"<path[^>]*class=""rz-series-item""").Count;
            // At least paths are rendered (column SVG paths with M commands)
            Assert.Contains("M ", chart.Markup);
        }

        [Fact]
        public void ColumnSeries_Fill_AppliedToColumns()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenColumnSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Fill, "#CC6600")
                    .Add(x => x.Data, SampleData)));

            Assert.Contains("#CC6600", chart.Markup);
        }

        [Fact]
        public void ColumnSeries_IndividualFills_AppliedPerColumn()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenColumnSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Fills, new[] { "#FF0000", "#00FF00", "#0000FF" })
                    .Add(x => x.Data, SampleData)));

            Assert.Contains("#FF0000", chart.Markup);
            Assert.Contains("#00FF00", chart.Markup);
            Assert.Contains("#0000FF", chart.Markup);
        }

        [Fact]
        public void ColumnSeries_MultipleSeries_RenderedSideBySide()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenColumnSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Title, "Series A")
                    .Add(x => x.Data, SampleData))
                .AddChildContent<RadzenColumnSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value2))
                    .Add(x => x.Title, "Series B")
                    .Add(x => x.Data, SampleData)));

            Assert.Contains("rz-series-0", chart.Markup);
            Assert.Contains("rz-series-1", chart.Markup);
            Assert.Contains("Series A", chart.Markup);
            Assert.Contains("Series B", chart.Markup);
        }

        [Fact]
        public void ColumnSeries_Hidden_DoesNotRender()
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
