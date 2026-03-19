using Bunit;
using Xunit;
using static Radzen.Blazor.Tests.ChartTestHelper;

namespace Radzen.Blazor.Tests
{
    public class BarSeriesTests
    {
        [Fact]
        public void BarSeries_Renders_ThreeBarPaths()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenBarSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Fill, "#336699")
                    .Add(x => x.Data, SampleData)));

            Assert.Contains("rz-bar-series rz-series-0", chart.Markup);
            var fillCount = System.Text.RegularExpressions.Regex.Matches(chart.Markup, "fill: #336699").Count;
            Assert.Equal(3, fillCount);
        }

        [Fact]
        public void BarSeries_CategoryAxis_ShowsCategoriesOnValueAxis()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenBarSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Data, SampleData)));

            // Bar chart swaps axes — categories on the value axis side
            Assert.Contains(">A</text>", chart.Markup);
            Assert.Contains(">B</text>", chart.Markup);
            Assert.Contains(">C</text>", chart.Markup);
        }

        [Fact]
        public void BarSeries_IndividualFills_Applied()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenBarSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Fills, new[] { "#FF0000", "#00FF00", "#0000FF" })
                    .Add(x => x.Data, SampleData)));

            Assert.Contains("fill: #FF0000", chart.Markup);
            Assert.Contains("fill: #00FF00", chart.Markup);
            Assert.Contains("fill: #0000FF", chart.Markup);
        }

        [Fact]
        public void BarSeries_Hidden_NoSeriesGroupRendered()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenBarSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Visible, false)
                    .Add(x => x.Data, SampleData)));

            Assert.DoesNotContain("rz-bar-series", chart.Markup);
        }

        [Fact]
        public void BarSeries_Title_InLegend()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenBarSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Title, "Sales")
                    .Add(x => x.Data, SampleData)));

            Assert.Contains(">Sales</span>", chart.Markup);
        }
    }
}
