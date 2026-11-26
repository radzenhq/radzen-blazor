using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using Xunit;
using static Radzen.Blazor.Tests.ChartTestHelper;

namespace Radzen.Blazor.Tests
{
    public class ChartZoomSeriesCullingTests
    {
        const int Count = 40;

        static readonly DataItem[] Items = Enumerable.Range(0, Count)
            .Select(i => new DataItem
            {
                Category = $"C{i:00}",
                Value = 10 + i % 7,
                Value2 = i,
                Min = 5 + i % 3,
                Max = 20 + i % 5,
                Open = 12 + i % 4,
                High = 20 + i % 4,
                Low = 8 + i % 4,
                Close = 14 + i % 4,
                LowerWhisker = 5,
                LowerQuartile = 8,
                Median = 12,
                UpperQuartile = 16,
                UpperWhisker = 20
            })
            .ToArray();

        static readonly Dictionary<string, (Action<ComponentParameterCollectionBuilder<RadzenChart>> Add, string Selector, int PerItem)> Series = new()
        {
            ["StackedColumn"] = (p => p.AddChildContent<RadzenStackedColumnSeries<DataItem>>(s => s
                .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                .Add(x => x.ValueProperty, nameof(DataItem.Value))
                .Add(x => x.Data, Items)), ".rz-column-series > path", 1),
            ["FullStackedColumn"] = (p => p.AddChildContent<RadzenFullStackedColumnSeries<DataItem>>(s => s
                .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                .Add(x => x.ValueProperty, nameof(DataItem.Value))
                .Add(x => x.Data, Items)), ".rz-full-stacked-column-series > path", 1),
            ["RangeColumn"] = (p => p.AddChildContent<RadzenRangeColumnSeries<DataItem>>(s => s
                .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                .Add(x => x.MinProperty, nameof(DataItem.Min))
                .Add(x => x.MaxProperty, nameof(DataItem.Max))
                .Add(x => x.Data, Items)), ".rz-range-column-series > path", 1),
            ["Waterfall"] = (p => p.AddChildContent<RadzenWaterfallSeries<DataItem>>(s => s
                .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                .Add(x => x.ValueProperty, nameof(DataItem.Value))
                .Add(x => x.Data, Items)), ".rz-waterfall-series > path", 1),
            ["Candlestick"] = (p => p.AddChildContent<RadzenCandlestickSeries<DataItem>>(s => s
                .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                .Add(x => x.OpenProperty, nameof(DataItem.Open))
                .Add(x => x.HighProperty, nameof(DataItem.High))
                .Add(x => x.LowProperty, nameof(DataItem.Low))
                .Add(x => x.CloseProperty, nameof(DataItem.Close))
                .Add(x => x.Data, Items)), ".rz-candlestick-series > path", 2),
            ["Ohlc"] = (p => p.AddChildContent<RadzenOhlcSeries<DataItem>>(s => s
                .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                .Add(x => x.OpenProperty, nameof(DataItem.Open))
                .Add(x => x.HighProperty, nameof(DataItem.High))
                .Add(x => x.LowProperty, nameof(DataItem.Low))
                .Add(x => x.CloseProperty, nameof(DataItem.Close))
                .Add(x => x.Data, Items)), ".rz-ohlc-series > path", 3),
            ["HighLow"] = (p => p.AddChildContent<RadzenHighLowSeries<DataItem>>(s => s
                .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                .Add(x => x.HighProperty, nameof(DataItem.High))
                .Add(x => x.LowProperty, nameof(DataItem.Low))
                .Add(x => x.Data, Items)), ".rz-highlow-series > path", 1),
            ["BoxPlot"] = (p => p.AddChildContent<RadzenBoxPlotSeries<DataItem>>(s => s
                .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                .Add(x => x.LowerWhiskerProperty, nameof(DataItem.LowerWhisker))
                .Add(x => x.LowerQuartileProperty, nameof(DataItem.LowerQuartile))
                .Add(x => x.MedianProperty, nameof(DataItem.Median))
                .Add(x => x.UpperQuartileProperty, nameof(DataItem.UpperQuartile))
                .Add(x => x.UpperWhiskerProperty, nameof(DataItem.UpperWhisker))
                .Add(x => x.Data, Items)), ".rz-box-plot-series > path", 5),
            ["Bubble"] = (p => p.AddChildContent<RadzenBubbleSeries<DataItem>>(s => s
                .Add(x => x.CategoryProperty, nameof(DataItem.Value2))
                .Add(x => x.ValueProperty, nameof(DataItem.Value))
                .Add(x => x.SizeProperty, nameof(DataItem.Max))
                .Add(x => x.Data, Items)), ".rz-bubble-series > circle", 1),
        };

        public static IEnumerable<object[]> SeriesNames => Series.Keys.Select(name => new object[] { name });

        static async Task<int> CountItems(TestContext ctx, string name, double viewStart, double viewEnd)
        {
            var (add, selector, perItem) = Series[name];

            var chart = ctx.RenderComponent<RadzenChart>(p =>
            {
                p.Add(x => x.AllowZoom, true);
                p.Add(x => x.ViewStart, viewStart);
                p.Add(x => x.ViewEnd, viewEnd);
                add(p);
            });

            await chart.InvokeAsync(() => chart.Instance.Resize(800, 300));

            var elements = chart.FindAll(selector).Count;

            Assert.Equal(0, elements % perItem);

            return elements / perItem;
        }

        [Theory]
        [MemberData(nameof(SeriesNames))]
        public async Task Series_FullView_RendersEveryItem(string name)
        {
            using var ctx = CreateChartContext();

            Assert.Equal(Count, await CountItems(ctx, name, 0, 1));
        }

        [Theory]
        [MemberData(nameof(SeriesNames))]
        public async Task Series_ZoomedToAQuarter_RendersOnlyItemsInView(string name)
        {
            using var ctx = CreateChartContext();

            var rendered = await CountItems(ctx, name, 0.5, 0.75);

            Assert.InRange(rendered, Count / 4 - 1, Count / 4 + 3);
        }

        [Theory]
        [MemberData(nameof(SeriesNames))]
        public async Task Series_ZoomedHalves_TogetherCoverEveryItem(string name)
        {
            using var ctx = CreateChartContext();

            var first = await CountItems(ctx, name, 0, 0.5);
            var second = await CountItems(ctx, name, 0.5, 1);

            Assert.InRange(first + second, Count, Count + 4);
        }

        static readonly Dictionary<string, (Action<ComponentParameterCollectionBuilder<RadzenChart>> Add, string Selector)> Lines = new()
        {
            ["StackedLine"] = (p => p.AddChildContent<RadzenStackedLineSeries<DataItem>>(s => s
                .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                .Add(x => x.ValueProperty, nameof(DataItem.Value))
                .Add(x => x.Data, Items)), ".rz-stacked-line-series path"),
            ["FullStackedLine"] = (p => p.AddChildContent<RadzenFullStackedLineSeries<DataItem>>(s => s
                .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                .Add(x => x.ValueProperty, nameof(DataItem.Value))
                .Add(x => x.Data, Items)), ".rz-full-stacked-line-series path"),
            ["StackedArea"] = (p => p.AddChildContent<RadzenStackedAreaSeries<DataItem>>(s => s
                .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                .Add(x => x.ValueProperty, nameof(DataItem.Value))
                .Add(x => x.Data, Items)), ".rz-area-series path"),
            ["FullStackedArea"] = (p => p.AddChildContent<RadzenFullStackedAreaSeries<DataItem>>(s => s
                .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                .Add(x => x.ValueProperty, nameof(DataItem.Value))
                .Add(x => x.Data, Items)), ".rz-full-stacked-area-series path"),
        };

        public static IEnumerable<object[]> LineNames => Lines.Keys.Select(name => new object[] { name });

        static async Task<int> CountLinePoints(TestContext ctx, string name, double viewStart, double viewEnd)
        {
            var (add, selector) = Lines[name];

            var chart = ctx.RenderComponent<RadzenChart>(p =>
            {
                p.Add(x => x.AllowZoom, true);
                p.Add(x => x.ViewStart, viewStart);
                p.Add(x => x.ViewEnd, viewEnd);
                add(p);
            });

            await chart.InvokeAsync(() => chart.Instance.Resize(800, 300));

            var line = chart.FindAll(selector).Last().GetAttribute("d")!;

            return System.Text.RegularExpressions.Regex.Matches(line, "L ").Count + 1;
        }

        [Theory]
        [MemberData(nameof(LineNames))]
        public async Task StackedLines_FullView_RenderEveryPoint(string name)
        {
            using var ctx = CreateChartContext();

            Assert.Equal(Count, await CountLinePoints(ctx, name, 0, 1));
        }

        [Theory]
        [MemberData(nameof(LineNames))]
        public async Task StackedLines_ZoomedToAQuarter_RenderOnlyThePointsAroundTheView(string name)
        {
            using var ctx = CreateChartContext();

            var rendered = await CountLinePoints(ctx, name, 0.5, 0.75);

            Assert.InRange(rendered, Count / 4, Count / 4 + 4);
        }
    }
}
