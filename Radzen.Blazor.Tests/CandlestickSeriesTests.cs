using Bunit;
using Xunit;
using static Radzen.Blazor.Tests.ChartTestHelper;

namespace Radzen.Blazor.Tests
{
    public class CandlestickSeriesTests
    {
        private static DataItem[] CandlestickData => new[]
        {
            new DataItem { Category = "Day1", Open = 10, High = 15, Low = 8, Close = 12 },
            new DataItem { Category = "Day2", Open = 12, High = 18, Low = 10, Close = 16 },
        };

        [Fact]
        public void CandlestickSeries_Renders_CandlestickClass()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenCandlestickSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.OpenProperty, nameof(DataItem.Open))
                    .Add(x => x.HighProperty, nameof(DataItem.High))
                    .Add(x => x.LowProperty, nameof(DataItem.Low))
                    .Add(x => x.CloseProperty, nameof(DataItem.Close))
                    .Add(x => x.Data, CandlestickData)));

            Assert.Contains("rz-candlestick-series", chart.Markup);
        }

        [Fact]
        public void CandlestickSeries_BullFill_Applied()
        {
            using var ctx = CreateChartContext();

            // Day1: close(12) > open(10) = bull
            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenCandlestickSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.OpenProperty, nameof(DataItem.Open))
                    .Add(x => x.HighProperty, nameof(DataItem.High))
                    .Add(x => x.LowProperty, nameof(DataItem.Low))
                    .Add(x => x.CloseProperty, nameof(DataItem.Close))
                    .Add(x => x.BullFill, "#00FF00")
                    .Add(x => x.Data, CandlestickData)));

            Assert.Contains("#00FF00", chart.Markup);
        }

        [Fact]
        public void CandlestickSeries_Hidden_DoesNotRender()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenCandlestickSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.OpenProperty, nameof(DataItem.Open))
                    .Add(x => x.HighProperty, nameof(DataItem.High))
                    .Add(x => x.LowProperty, nameof(DataItem.Low))
                    .Add(x => x.CloseProperty, nameof(DataItem.Close))
                    .Add(x => x.Visible, false)
                    .Add(x => x.Data, CandlestickData)));

            Assert.DoesNotContain("rz-candlestick-series", chart.Markup);
        }
    }
}
