using Bunit;
using Xunit;
using static Radzen.Blazor.Tests.ChartTestHelper;

namespace Radzen.Blazor.Tests
{
    public class BarSeriesTests
    {
        [Fact]
        public void BarSeries_Renders_BarClass()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenBarSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Data, SampleData)));

            Assert.Contains("rz-bar-series", chart.Markup);
        }

        [Fact]
        public void BarSeries_Fill_AppliedToBars()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenBarSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Fill, "#336699")
                    .Add(x => x.Data, SampleData)));

            Assert.Contains("#336699", chart.Markup);
        }

        [Fact]
        public void BarSeries_IndividualFills()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenBarSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Fills, new[] { "#FF0000", "#00FF00", "#0000FF" })
                    .Add(x => x.Data, SampleData)));

            Assert.Contains("#FF0000", chart.Markup);
            Assert.Contains("#00FF00", chart.Markup);
            Assert.Contains("#0000FF", chart.Markup);
        }

        [Fact]
        public void BarSeries_Hidden_DoesNotRender()
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

            Assert.Contains("Sales", chart.Markup);
        }
    }
}
