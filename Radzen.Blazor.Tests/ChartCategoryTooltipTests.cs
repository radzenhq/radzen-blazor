using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Bunit;
using Xunit;
using static Radzen.Blazor.Tests.ChartTestHelper;

namespace Radzen.Blazor.Tests
{
    public class ChartCategoryTooltipTests
    {
        class Candle
        {
            public DateTime Time { get; set; }
            public double Open { get; set; }
            public double High { get; set; }
            public double Low { get; set; }
            public double Close { get; set; }
        }

        static readonly Candle[] Candles = Enumerable.Range(0, 20)
            .Select(i => new Candle { Time = new DateTime(2024, 1, 1).AddDays(i), Open = 50 + i, High = 56 + i, Low = 48 + i, Close = 53 + i })
            .ToArray();

        static async Task<IRenderedComponent<RadzenChart>> RenderCandlesticks(TestContext ctx, bool crosshair)
        {
            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenCandlestickSeries<Candle>>(s => s
                    .Add(x => x.CategoryProperty, nameof(Candle.Time))
                    .Add(x => x.OpenProperty, nameof(Candle.Open))
                    .Add(x => x.HighProperty, nameof(Candle.High))
                    .Add(x => x.LowProperty, nameof(Candle.Low))
                    .Add(x => x.CloseProperty, nameof(Candle.Close))
                    .Add(x => x.Data, Candles))
                .AddChildContent<RadzenCategoryAxis>(a => a
                    .AddChildContent<RadzenAxisCrosshair>(c => c
                        .Add(x => x.Visible, crosshair))));

            await chart.InvokeAsync(() => chart.Instance.Resize(800, 400));

            return chart;
        }

        static async Task<IRenderedComponent<RadzenChart>> RenderOhlc(TestContext ctx, bool crosshair)
        {
            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenOhlcSeries<Candle>>(s => s
                    .Add(x => x.CategoryProperty, nameof(Candle.Time))
                    .Add(x => x.OpenProperty, nameof(Candle.Open))
                    .Add(x => x.HighProperty, nameof(Candle.High))
                    .Add(x => x.LowProperty, nameof(Candle.Low))
                    .Add(x => x.CloseProperty, nameof(Candle.Close))
                    .Add(x => x.Data, Candles))
                .AddChildContent<RadzenCategoryAxis>(a => a
                    .AddChildContent<RadzenAxisCrosshair>(c => c
                        .Add(x => x.Visible, crosshair))));

            await chart.InvokeAsync(() => chart.Instance.Resize(800, 400));

            return chart;
        }

        static (double X, double Y) AwayFromCandles(IRenderedComponent<RadzenChart> chart)
        {
            var translate = Regex.Match(chart.Markup, @"translate\(([\d.]+),\s*([\d.]+)\)");
            var left = double.Parse(translate.Groups[1].Value, CultureInfo.InvariantCulture);
            var top = double.Parse(translate.Groups[2].Value, CultureInfo.InvariantCulture);

            return (left + chart.Instance.CategoryScale.OutputSize / 2 + 9, top + 6);
        }

        static int TooltipOpens(TestContext ctx) => ctx.JSInterop.Invocations.Count(i => i.Identifier == "Radzen.openChartTooltip");

        [Fact]
        public async Task CandlestickSeries_AxisTooltip_ShowsTheNearestCandleAwayFromTheCandles()
        {
            using var ctx = CreateChartContext();

            var chart = await RenderCandlesticks(ctx, crosshair: true);
            var (x, y) = AwayFromCandles(chart);

            await chart.InvokeAsync(() => chart.Instance.MouseMove(x, y));

            Assert.Contains("rz-chart-category-tooltip", chart.Markup);
            Assert.Contains("Open", chart.Markup);
        }

        [Fact]
        public async Task OhlcSeries_AxisTooltip_ShowsTheNearestBarAwayFromTheBars()
        {
            using var ctx = CreateChartContext();

            var chart = await RenderOhlc(ctx, crosshair: true);
            var (x, y) = AwayFromCandles(chart);

            await chart.InvokeAsync(() => chart.Instance.MouseMove(x, y));

            Assert.Contains("rz-chart-category-tooltip", chart.Markup);
        }

        [Fact]
        public async Task CandlestickSeries_PointTooltip_IsNotShownAwayFromTheCandles()
        {
            using var ctx = CreateChartContext();

            var chart = await RenderCandlesticks(ctx, crosshair: false);
            var (x, y) = AwayFromCandles(chart);

            await chart.InvokeAsync(() => chart.Instance.MouseMove(x, y));

            Assert.DoesNotContain("rz-chart-category-tooltip", chart.Markup);
            Assert.Equal(0, TooltipOpens(ctx));
        }

        [Fact]
        public async Task OhlcSeries_PointTooltip_IsNotShownAwayFromTheBars()
        {
            using var ctx = CreateChartContext();

            var chart = await RenderOhlc(ctx, crosshair: false);
            var (x, y) = AwayFromCandles(chart);

            await chart.InvokeAsync(() => chart.Instance.MouseMove(x, y));

            Assert.DoesNotContain("rz-chart-category-tooltip", chart.Markup);
            Assert.Equal(0, TooltipOpens(ctx));
        }
    }
}
