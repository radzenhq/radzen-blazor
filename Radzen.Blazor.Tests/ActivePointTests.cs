using System.Linq;
using Bunit;
using Xunit;
using static Radzen.Blazor.Tests.ChartTestHelper;

namespace Radzen.Blazor.Tests
{
    public class ActivePointTests
    {
        static DataItem[] OhlcData => new[]
        {
            new DataItem { Category = "A", Value = 10, Open = 9, High = 12, Low = 8, Close = 11, Min = 8, Max = 12, LowerWhisker = 8, LowerQuartile = 9, Median = 10, UpperQuartile = 11, UpperWhisker = 12 },
            new DataItem { Category = "B", Value = 20, Open = 18, High = 22, Low = 17, Close = 21, Min = 17, Max = 22, LowerWhisker = 17, LowerQuartile = 18, Median = 20, UpperQuartile = 21, UpperWhisker = 22 },
            new DataItem { Category = "C", Value = 15, Open = 14, High = 17, Low = 13, Close = 16, Min = 13, Max = 17, LowerWhisker = 13, LowerQuartile = 14, Median = 15, UpperQuartile = 16, UpperWhisker = 17 },
        };

        [Fact]
        public async System.Threading.Tasks.Task LineSeries_Hover_ShowsActivePoint()
        {
            using var ctx = CreateChartContext();
            ctx.JSInterop.SetupVoid("Radzen.openChartTooltip", _ => true);
            ctx.RenderComponent<RadzenChartTooltip>();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenLineSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Data, SampleData)));
            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));

            // Probe a grid until the cursor lands within hover tolerance of a data point.
            for (var x = 40; x <= 280 && !chart.Markup.Contains("rz-active-point"); x += 8)
            {
                for (var y = 24; y <= 280 && !chart.Markup.Contains("rz-active-point"); y += 8)
                {
                    var cx = x;
                    var cy = y;
                    await chart.InvokeAsync(() => chart.Instance.MouseMove(cx, cy));
                }
            }

            Assert.Contains("rz-active-point", chart.Markup);
        }

        [Fact]
        public async System.Threading.Tasks.Task ActivePoint_InheritsSeriesMarkerShape()
        {
            using var ctx = CreateChartContext();
            ctx.JSInterop.SetupVoid("Radzen.openChartTooltip", _ => true);
            ctx.RenderComponent<RadzenChartTooltip>();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenLineSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Data, SampleData)
                    .AddChildContent<RadzenMarkers>(m => m.Add(x => x.MarkerType, MarkerType.Square))));
            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));

            for (var x = 40; x <= 280 && !chart.Markup.Contains("rz-active-point"); x += 8)
            {
                for (var y = 24; y <= 280 && !chart.Markup.Contains("rz-active-point"); y += 8)
                {
                    var cx = x;
                    var cy = y;
                    await chart.InvokeAsync(() => chart.Instance.MouseMove(cx, cy));
                }
            }

            // A square-marker series highlights with a square path, not a circle; the halo stays circular.
            Assert.Contains("<path class=\"rz-active-point-dot\"", chart.Markup);
            Assert.DoesNotContain("<circle class=\"rz-active-point-dot\"", chart.Markup);
            Assert.Contains("<circle class=\"rz-active-point-halo\"", chart.Markup);
        }

        [Fact]
        public async System.Threading.Tasks.Task ActivePoint_DefaultsToCircleDot()
        {
            using var ctx = CreateChartContext();
            ctx.JSInterop.SetupVoid("Radzen.openChartTooltip", _ => true);
            ctx.RenderComponent<RadzenChartTooltip>();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenLineSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Data, SampleData)));
            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));

            for (var x = 40; x <= 280 && !chart.Markup.Contains("rz-active-point"); x += 8)
            {
                for (var y = 24; y <= 280 && !chart.Markup.Contains("rz-active-point"); y += 8)
                {
                    var cx = x;
                    var cy = y;
                    await chart.InvokeAsync(() => chart.Instance.MouseMove(cx, cy));
                }
            }

            Assert.Contains("<circle class=\"rz-active-point-dot\"", chart.Markup);
        }

        [Fact]
        public async System.Threading.Tasks.Task CandlestickSeries_Hover_ShowsNoActivePoint()
        {
            using var ctx = CreateChartContext();
            ctx.JSInterop.SetupVoid("Radzen.openChartTooltip", _ => true);
            ctx.RenderComponent<RadzenChartTooltip>();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenCandlestickSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.OpenProperty, nameof(DataItem.Open))
                    .Add(x => x.HighProperty, nameof(DataItem.High))
                    .Add(x => x.LowProperty, nameof(DataItem.Low))
                    .Add(x => x.CloseProperty, nameof(DataItem.Close))
                    .Add(x => x.Data, OhlcData)));
            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));

            // Sweep the same grid a line series responds to - a candle must never show the dot.
            for (var x = 40; x <= 280; x += 8)
            {
                for (var y = 24; y <= 280; y += 8)
                {
                    var cx = x;
                    var cy = y;
                    await chart.InvokeAsync(() => chart.Instance.MouseMove(cx, cy));
                    Assert.DoesNotContain("rz-active-point", chart.Markup);
                }
            }
        }

        [Fact]
        public async System.Threading.Tasks.Task RangeStyleSeries_OptOutOfActivePoint()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p =>
            {
                p.AddChildContent<RadzenCandlestickSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.OpenProperty, nameof(DataItem.Open))
                    .Add(x => x.HighProperty, nameof(DataItem.High))
                    .Add(x => x.LowProperty, nameof(DataItem.Low))
                    .Add(x => x.CloseProperty, nameof(DataItem.Close))
                    .Add(x => x.Data, OhlcData));
                p.AddChildContent<RadzenOhlcSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.OpenProperty, nameof(DataItem.Open))
                    .Add(x => x.HighProperty, nameof(DataItem.High))
                    .Add(x => x.LowProperty, nameof(DataItem.Low))
                    .Add(x => x.CloseProperty, nameof(DataItem.Close))
                    .Add(x => x.Data, OhlcData));
                p.AddChildContent<RadzenHighLowSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.HighProperty, nameof(DataItem.High))
                    .Add(x => x.LowProperty, nameof(DataItem.Low))
                    .Add(x => x.Data, OhlcData));
                p.AddChildContent<RadzenBoxPlotSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.LowerWhiskerProperty, nameof(DataItem.LowerWhisker))
                    .Add(x => x.LowerQuartileProperty, nameof(DataItem.LowerQuartile))
                    .Add(x => x.MedianProperty, nameof(DataItem.Median))
                    .Add(x => x.UpperQuartileProperty, nameof(DataItem.UpperQuartile))
                    .Add(x => x.UpperWhiskerProperty, nameof(DataItem.UpperWhisker))
                    .Add(x => x.Data, OhlcData));
                p.AddChildContent<RadzenLineSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Data, SampleData));
            });
            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));

            var flags = chart.Instance.Series.ToDictionary(s => s.GetType().Name, s => s.ShowActivePoint);

            Assert.False(flags["RadzenCandlestickSeries`1"]);
            Assert.False(flags["RadzenOhlcSeries`1"]);
            Assert.False(flags["RadzenHighLowSeries`1"]);
            Assert.False(flags["RadzenBoxPlotSeries`1"]);
            Assert.True(flags["RadzenLineSeries`1"]);
        }
    }
}
