using Bunit;
using Xunit;
using static Radzen.Blazor.Tests.ChartTestHelper;

namespace Radzen.Blazor.Tests
{
    public class WaterfallSeriesTests
    {
        [Fact]
        public void WaterfallSeries_Renders_WaterfallClass()
        {
            using var ctx = CreateChartContext();

            var data = new[]
            {
                new DataItem { Category = "Start", Value = 100, IsSummary = true },
                new DataItem { Category = "Add", Value = 30 },
                new DataItem { Category = "Sub", Value = -10 },
            };

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenWaterfallSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.SummaryProperty, nameof(DataItem.IsSummary))
                    .Add(x => x.Data, data)));

            Assert.Contains("rz-waterfall-series", chart.Markup);
        }

        [Fact]
        public void WaterfallSeries_CustomPositiveFill_Applied()
        {
            using var ctx = CreateChartContext();

            var data = new[]
            {
                new DataItem { Category = "Start", Value = 100, IsSummary = true },
                new DataItem { Category = "Up", Value = 30 },
            };

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenWaterfallSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.SummaryProperty, nameof(DataItem.IsSummary))
                    .Add(x => x.PositiveFill, "#00CC00")
                    .Add(x => x.SummaryFill, "#0000CC")
                    .Add(x => x.Data, data)));

            Assert.Contains("#00CC00", chart.Markup);
            Assert.Contains("#0000CC", chart.Markup);
        }

        [Fact]
        public void WaterfallSeries_Hidden_DoesNotRender()
        {
            using var ctx = CreateChartContext();

            var data = new[]
            {
                new DataItem { Category = "Start", Value = 100, IsSummary = true },
                new DataItem { Category = "Add", Value = 30 },
            };

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenWaterfallSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.SummaryProperty, nameof(DataItem.IsSummary))
                    .Add(x => x.Visible, false)
                    .Add(x => x.Data, data)));

            Assert.DoesNotContain("rz-waterfall-series", chart.Markup);
        }

        [Fact]
        public void HorizontalWaterfallSeries_Renders_BarClass()
        {
            using var ctx = CreateChartContext();

            var data = new[]
            {
                new DataItem { Category = "Start", Value = 100, IsSummary = true },
                new DataItem { Category = "Change", Value = 20 },
            };

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenHorizontalWaterfallSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.SummaryProperty, nameof(DataItem.IsSummary))
                    .Add(x => x.Data, data)));

            Assert.Contains("rz-horizontal-waterfall-series", chart.Markup);
        }
    }
}
