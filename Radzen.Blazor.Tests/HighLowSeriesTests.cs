using Bunit;
using Xunit;
using static Radzen.Blazor.Tests.ChartTestHelper;

namespace Radzen.Blazor.Tests
{
    public class HighLowSeriesTests
    {
        private static DataItem[] HighLowData => new[]
        {
            new DataItem { Category = "Mon", High = 20, Low = 10 },
            new DataItem { Category = "Tue", High = 25, Low = 12 },
        };

        [Fact]
        public void HighLowSeries_Renders_HighLowClass()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenHighLowSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.HighProperty, nameof(DataItem.High))
                    .Add(x => x.LowProperty, nameof(DataItem.Low))
                    .Add(x => x.Data, HighLowData)));

            Assert.Contains("rz-highlow-series", chart.Markup);
        }

        [Fact]
        public void HighLowSeries_CustomStroke_Applied()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenHighLowSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.HighProperty, nameof(DataItem.High))
                    .Add(x => x.LowProperty, nameof(DataItem.Low))
                    .Add(x => x.Stroke, "#FF6600")
                    .Add(x => x.Data, HighLowData)));

            Assert.Contains("#FF6600", chart.Markup);
        }

        [Fact]
        public void HighLowSeries_Hidden_DoesNotRender()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenHighLowSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.HighProperty, nameof(DataItem.High))
                    .Add(x => x.LowProperty, nameof(DataItem.Low))
                    .Add(x => x.Visible, false)
                    .Add(x => x.Data, HighLowData)));

            Assert.DoesNotContain("rz-highlow-series", chart.Markup);
        }
    }
}
