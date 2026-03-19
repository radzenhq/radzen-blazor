using Bunit;
using Xunit;
using static Radzen.Blazor.Tests.ChartTestHelper;

namespace Radzen.Blazor.Tests
{
    public class OhlcSeriesTests
    {
        private static DataItem[] OhlcData => new[]
        {
            new DataItem { Category = "Day1", Open = 10, High = 15, Low = 8, Close = 12 },
            new DataItem { Category = "Day2", Open = 12, High = 18, Low = 10, Close = 16 },
        };

        [Fact]
        public void OhlcSeries_Renders_OhlcClass()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenOhlcSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.OpenProperty, nameof(DataItem.Open))
                    .Add(x => x.HighProperty, nameof(DataItem.High))
                    .Add(x => x.LowProperty, nameof(DataItem.Low))
                    .Add(x => x.CloseProperty, nameof(DataItem.Close))
                    .Add(x => x.Data, OhlcData)));

            Assert.Contains("rz-ohlc-series", chart.Markup);
        }

        [Fact]
        public void OhlcSeries_CustomBullBearStrokes()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenOhlcSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.OpenProperty, nameof(DataItem.Open))
                    .Add(x => x.HighProperty, nameof(DataItem.High))
                    .Add(x => x.LowProperty, nameof(DataItem.Low))
                    .Add(x => x.CloseProperty, nameof(DataItem.Close))
                    .Add(x => x.BullStroke, "#00CC00")
                    .Add(x => x.BearStroke, "#CC0000")
                    .Add(x => x.Data, OhlcData)));

            // Bull (close > open): Day1 close=12 > open=10, Day2 close=16 > open=12
            Assert.Contains("#00CC00", chart.Markup);
        }

        [Fact]
        public void OhlcSeries_Hidden_DoesNotRender()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenOhlcSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.OpenProperty, nameof(DataItem.Open))
                    .Add(x => x.HighProperty, nameof(DataItem.High))
                    .Add(x => x.LowProperty, nameof(DataItem.Low))
                    .Add(x => x.CloseProperty, nameof(DataItem.Close))
                    .Add(x => x.Visible, false)
                    .Add(x => x.Data, OhlcData)));

            Assert.DoesNotContain("rz-ohlc-series", chart.Markup);
        }
    }
}
