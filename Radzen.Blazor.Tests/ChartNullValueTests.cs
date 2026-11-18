using System;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using Xunit;
using static Radzen.Blazor.Tests.ChartTestHelper;

namespace Radzen.Blazor.Tests
{
    public class ChartNullValueTests
    {
        public class NullableItem
        {
            public DateTime Date { get; set; }
            public DateTime? NullableDate { get; set; }
            public int Whole { get; set; }
            public string Name { get; set; } = "";
            public double? X { get; set; }
            public double? Value { get; set; }
            public int? Count { get; set; }
            public double? Low { get; set; }
            public double? High { get; set; }
            public double? Open { get; set; }
            public double? Close { get; set; }
        }

        const int Total = 6;
        const int Empty = 2;

        static NullableItem[] Data => Enumerable.Range(0, Total).Select(i => new NullableItem
        {
            Date = new DateTime(2026, 1, 1).AddDays(i),
            NullableDate = i < Empty ? null : new DateTime(2026, 1, 1).AddDays(i),
            Whole = i + 1,
            Name = $"N{i}",
            X = i < Empty ? null : i,
            Value = i < Empty ? null : i * 2 - 5,
            Count = i < Empty ? null : i,
            Low = i < Empty ? null : i,
            High = i < Empty ? null : i + 3,
            Open = i < Empty ? null : i + 1,
            Close = i < Empty ? null : i + 2,
        }).ToArray();

        static async Task<IRenderedComponent<RadzenChart>> Render(TestContext ctx, Action<ComponentParameterCollectionBuilder<RadzenChart>> series)
        {
            ctx.JSInterop.SetupVoid("Radzen.openChartTooltip", _ => true);
            ctx.RenderComponent<RadzenChartTooltip>();

            var chart = ctx.RenderComponent<RadzenChart>(series);

            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));

            for (var x = 20; x < 400; x += 20)
            {
                for (var y = 20; y < 300; y += 40)
                {
                    await chart.InvokeAsync(() => chart.Instance.MouseMove(x, y));
                }
            }

            Assert.DoesNotContain("NaN", chart.Markup);

            return chart;
        }

        [Fact]
        public async Task ColumnSeries_SkipsNullValues()
        {
            using var ctx = CreateChartContext();

            var chart = await Render(ctx, p => p.AddChildContent<RadzenColumnSeries<NullableItem>>(s => s
                .Add(x => x.CategoryProperty, nameof(NullableItem.Date))
                .Add(x => x.ValueProperty, nameof(NullableItem.Value))
                .Add(x => x.Data, Data)));

            Assert.Equal(Total - Empty, chart.FindAll("g.rz-column-series path").Count);
        }

        [Fact]
        public async Task ColumnSeries_SkipsNullIntegerValues()
        {
            using var ctx = CreateChartContext();

            var chart = await Render(ctx, p => p.AddChildContent<RadzenColumnSeries<NullableItem>>(s => s
                .Add(x => x.CategoryProperty, nameof(NullableItem.Name))
                .Add(x => x.ValueProperty, nameof(NullableItem.Count))
                .Add(x => x.Data, Data)));

            Assert.Equal(Total - Empty, chart.FindAll("g.rz-column-series path").Count);
        }

        [Fact]
        public async Task BarSeries_SkipsNullValues()
        {
            using var ctx = CreateChartContext();

            var chart = await Render(ctx, p => p.AddChildContent<RadzenBarSeries<NullableItem>>(s => s
                .Add(x => x.CategoryProperty, nameof(NullableItem.Name))
                .Add(x => x.ValueProperty, nameof(NullableItem.Value))
                .Add(x => x.Data, Data)));

            Assert.Equal(Total - Empty, chart.FindAll("g.rz-bar-series path").Count);
        }

        [Fact]
        public async Task ValueScale_IgnoresNullValues()
        {
            using var ctx = CreateChartContext();

            var chart = await Render(ctx, p => p.AddChildContent<RadzenLineSeries<NullableItem>>(s => s
                .Add(x => x.CategoryProperty, nameof(NullableItem.Date))
                .Add(x => x.ValueProperty, nameof(NullableItem.High))
                .Add(x => x.Data, Data)));

            Assert.True(chart.Instance.ValueScale.Input.Start > 0);
        }

        [Fact]
        public async Task LineSeries_HoverOverNullValuesDoesNotThrow()
        {
            using var ctx = CreateChartContext();

            var chart = await Render(ctx, p => p.AddChildContent<RadzenLineSeries<NullableItem>>(s => s
                .Add(x => x.CategoryProperty, nameof(NullableItem.Date))
                .Add(x => x.ValueProperty, nameof(NullableItem.Value))
                .Add(x => x.Data, Data)));

            Assert.Single(chart.FindAll("g.rz-line-series path"));
        }

        [Theory]
        [InlineData(Interpolation.Line)]
        [InlineData(Interpolation.Spline)]
        [InlineData(Interpolation.Step)]
        public async Task AreaSeries_LeavesGapForNullValues(Interpolation interpolation)
        {
            using var ctx = CreateChartContext();
            var data = Data;
            data[4].Value = null;

            var chart = await Render(ctx, p => p.AddChildContent<RadzenAreaSeries<NullableItem>>(s => s
                .Add(x => x.CategoryProperty, nameof(NullableItem.Date))
                .Add(x => x.ValueProperty, nameof(NullableItem.Value))
                .Add(x => x.Interpolation, interpolation)
                .Add(x => x.Data, data)));

            var area = chart.Find("g.rz-area-series path").GetAttribute("d");

            Assert.Equal(2, area!.Split('M', StringSplitOptions.RemoveEmptyEntries).Length);
        }

        [Theory]
        [InlineData(Interpolation.Line)]
        [InlineData(Interpolation.Spline)]
        [InlineData(Interpolation.Step)]
        public async Task RangeAreaSeries_StartsAtFirstValue(Interpolation interpolation)
        {
            using var ctx = CreateChartContext();

            var chart = await Render(ctx, p => p.AddChildContent<RadzenRangeAreaSeries<NullableItem>>(s => s
                .Add(x => x.CategoryProperty, nameof(NullableItem.Date))
                .Add(x => x.MinProperty, nameof(NullableItem.Low))
                .Add(x => x.MaxProperty, nameof(NullableItem.High))
                .Add(x => x.Interpolation, interpolation)
                .Add(x => x.Data, Data)));

            Assert.Equal(3, chart.FindAll("g.rz-range-area-series path").Count);
            Assert.Equal(new DateTime(2026, 1, 1).Ticks, chart.Instance.CategoryScale.Input.Start);
        }

        [Fact]
        public async Task RangeAreaSeries_AllNullRendersNothing()
        {
            using var ctx = CreateChartContext();
            var data = Data;

            foreach (var item in data)
            {
                item.Low = null;
                item.High = null;
            }

            var chart = await Render(ctx, p => p.AddChildContent<RadzenRangeAreaSeries<NullableItem>>(s => s
                .Add(x => x.CategoryProperty, nameof(NullableItem.Date))
                .Add(x => x.MinProperty, nameof(NullableItem.Low))
                .Add(x => x.MaxProperty, nameof(NullableItem.High))
                .Add(x => x.Data, data)));

            Assert.Empty(chart.FindAll("g.rz-range-area-series path"));
        }

        [Fact]
        public async Task RangeColumnSeries_SkipsNullValues()
        {
            using var ctx = CreateChartContext();

            var chart = await Render(ctx, p => p.AddChildContent<RadzenRangeColumnSeries<NullableItem>>(s => s
                .Add(x => x.CategoryProperty, nameof(NullableItem.Name))
                .Add(x => x.MinProperty, nameof(NullableItem.Low))
                .Add(x => x.MaxProperty, nameof(NullableItem.High))
                .Add(x => x.Data, Data)));

            Assert.Equal(Total - Empty, chart.FindAll("g.rz-range-column-series path").Count);
        }

        [Fact]
        public async Task RangeBarSeries_SkipsNullValues()
        {
            using var ctx = CreateChartContext();

            var chart = await Render(ctx, p => p.AddChildContent<RadzenRangeBarSeries<NullableItem>>(s => s
                .Add(x => x.CategoryProperty, nameof(NullableItem.Name))
                .Add(x => x.MinProperty, nameof(NullableItem.Low))
                .Add(x => x.MaxProperty, nameof(NullableItem.High))
                .Add(x => x.Data, Data)));

            Assert.Equal(Total - Empty, chart.FindAll("g.rz-range-bar-series path").Count);
        }

        [Fact]
        public async Task CandlestickSeries_SkipsNullValues()
        {
            using var ctx = CreateChartContext();

            var chart = await Render(ctx, p => p.AddChildContent<RadzenCandlestickSeries<NullableItem>>(s => s
                .Add(x => x.CategoryProperty, nameof(NullableItem.Date))
                .Add(x => x.OpenProperty, nameof(NullableItem.Open))
                .Add(x => x.HighProperty, nameof(NullableItem.High))
                .Add(x => x.LowProperty, nameof(NullableItem.Low))
                .Add(x => x.CloseProperty, nameof(NullableItem.Close))
                .Add(x => x.Data, Data)));

            Assert.Equal((Total - Empty) * 2, chart.FindAll("g.rz-candlestick-series path").Count);
        }

        [Fact]
        public async Task ScatterSeries_SkipsNullValues()
        {
            using var ctx = CreateChartContext();

            var chart = await Render(ctx, p => p.AddChildContent<RadzenScatterSeries<NullableItem>>(s => s
                .Add(x => x.CategoryProperty, nameof(NullableItem.X))
                .Add(x => x.ValueProperty, nameof(NullableItem.Value))
                .Add(x => x.Data, Data)));

            Assert.Equal(Empty, chart.Instance.CategoryScale.Input.Start);
        }

        [Fact]
        public async Task StackedColumnSeries_TreatsNullAsZero()
        {
            using var ctx = CreateChartContext();

            var chart = await Render(ctx, p => p
                .AddChildContent<RadzenStackedColumnSeries<NullableItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(NullableItem.Name))
                    .Add(x => x.ValueProperty, nameof(NullableItem.High))
                    .Add(x => x.Data, Data))
                .AddChildContent<RadzenStackedColumnSeries<NullableItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(NullableItem.Name))
                    .Add(x => x.ValueProperty, nameof(NullableItem.Low))
                    .Add(x => x.Data, Data)));

            Assert.Equal(Total * 2, chart.FindAll("g.rz-stacked-column-series path, g.rz-column-series path").Count);
        }

        [Fact]
        public async Task LineSeries_SkipsNullNumericCategory()
        {
            using var ctx = CreateChartContext();

            var chart = await Render(ctx, p => p.AddChildContent<RadzenLineSeries<NullableItem>>(s => s
                .Add(x => x.CategoryProperty, nameof(NullableItem.X))
                .Add(x => x.ValueProperty, nameof(NullableItem.High))
                .Add(x => x.Data, Data)));

            Assert.Equal(Empty, chart.Instance.CategoryScale.Input.Start);
        }

        [Fact]
        public async Task SyncedCharts_WithNullValuesDoNotThrow()
        {
            using var ctx = CreateChartContext();
            var group = $"test-{Guid.NewGuid():N}";

            var bands = await Render(ctx, p => p
                .Add(x => x.SyncGroup, group)
                .AddChildContent<RadzenRangeAreaSeries<NullableItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(NullableItem.Date))
                    .Add(x => x.MinProperty, nameof(NullableItem.Low))
                    .Add(x => x.MaxProperty, nameof(NullableItem.High))
                    .Add(x => x.Data, Data)));

            var histogram = await Render(ctx, p => p
                .Add(x => x.SyncGroup, group)
                .AddChildContent<RadzenColumnSeries<NullableItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(NullableItem.Date))
                    .Add(x => x.ValueProperty, nameof(NullableItem.Value))
                    .Add(x => x.Data, Data)));

            Assert.DoesNotContain("NaN", bands.Markup);
            Assert.DoesNotContain("NaN", histogram.Markup);
        }

        [Fact]
        public async Task OhlcSeries_SkipsNullValues()
        {
            using var ctx = CreateChartContext();

            var chart = await Render(ctx, p => p.AddChildContent<RadzenOhlcSeries<NullableItem>>(s => s
                .Add(x => x.CategoryProperty, nameof(NullableItem.Date))
                .Add(x => x.OpenProperty, nameof(NullableItem.Open))
                .Add(x => x.HighProperty, nameof(NullableItem.High))
                .Add(x => x.LowProperty, nameof(NullableItem.Low))
                .Add(x => x.CloseProperty, nameof(NullableItem.Close))
                .Add(x => x.Data, Data)));

            Assert.Equal((Total - Empty) * 3, chart.FindAll("g.rz-ohlc-series path").Count);
        }

        [Fact]
        public async Task HighLowSeries_SkipsNullValues()
        {
            using var ctx = CreateChartContext();

            var chart = await Render(ctx, p => p.AddChildContent<RadzenHighLowSeries<NullableItem>>(s => s
                .Add(x => x.CategoryProperty, nameof(NullableItem.Date))
                .Add(x => x.HighProperty, nameof(NullableItem.High))
                .Add(x => x.LowProperty, nameof(NullableItem.Low))
                .Add(x => x.Data, Data)));

            Assert.Equal(Total - Empty, chart.FindAll("g.rz-highlow-series path").Count);
        }

        [Fact]
        public async Task BoxPlotSeries_SkipsNullValues()
        {
            using var ctx = CreateChartContext();

            var chart = await Render(ctx, p => p.AddChildContent<RadzenBoxPlotSeries<NullableItem>>(s => s
                .Add(x => x.CategoryProperty, nameof(NullableItem.Name))
                .Add(x => x.LowerWhiskerProperty, nameof(NullableItem.Low))
                .Add(x => x.LowerQuartileProperty, nameof(NullableItem.Open))
                .Add(x => x.MedianProperty, nameof(NullableItem.Close))
                .Add(x => x.UpperQuartileProperty, nameof(NullableItem.High))
                .Add(x => x.UpperWhiskerProperty, nameof(NullableItem.High))
                .Add(x => x.Data, Data)));

            Assert.Equal((Total - Empty) * 5, chart.FindAll("g.rz-box-plot-series path").Count);
        }

        [Fact]
        public async Task WaterfallSeries_TreatsNullAsZero()
        {
            using var ctx = CreateChartContext();

            var chart = await Render(ctx, p => p.AddChildContent<RadzenWaterfallSeries<NullableItem>>(s => s
                .Add(x => x.CategoryProperty, nameof(NullableItem.Name))
                .Add(x => x.ValueProperty, nameof(NullableItem.Value))
                .Add(x => x.Data, Data)));

            Assert.Equal(Total, chart.FindAll("g.rz-waterfall-series path").Count);
        }

        [Fact]
        public async Task HorizontalWaterfallSeries_TreatsNullAsZero()
        {
            using var ctx = CreateChartContext();

            var chart = await Render(ctx, p => p.AddChildContent<RadzenHorizontalWaterfallSeries<NullableItem>>(s => s
                .Add(x => x.CategoryProperty, nameof(NullableItem.Name))
                .Add(x => x.ValueProperty, nameof(NullableItem.Value))
                .Add(x => x.Data, Data)));

            Assert.Equal(Total, chart.FindAll("g.rz-horizontal-waterfall-series path").Count);
        }

        [Fact]
        public async Task ColumnSeries_SkipsNullDateCategory()
        {
            using var ctx = CreateChartContext();

            var chart = await Render(ctx, p => p.AddChildContent<RadzenColumnSeries<NullableItem>>(s => s
                .Add(x => x.CategoryProperty, nameof(NullableItem.NullableDate))
                .Add(x => x.ValueProperty, nameof(NullableItem.Whole))
                .Add(x => x.Data, Data)));

            Assert.Equal(Total - Empty, chart.FindAll("g.rz-column-series path").Count);
        }

        [Fact]
        public async Task LineSeries_SkipsNullDateCategory()
        {
            using var ctx = CreateChartContext();

            var chart = await Render(ctx, p => p.AddChildContent<RadzenLineSeries<NullableItem>>(s => s
                .Add(x => x.CategoryProperty, nameof(NullableItem.NullableDate))
                .Add(x => x.ValueProperty, nameof(NullableItem.Whole))
                .Add(x => x.Data, Data)));

            Assert.Equal(1, chart.FindAll("g.rz-line-series path").Count);
        }

        [Fact]
        public async Task StackedBarSeries_TreatsNullAsZero()
        {
            using var ctx = CreateChartContext();

            var chart = await Render(ctx, p => p
                .AddChildContent<RadzenStackedBarSeries<NullableItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(NullableItem.Name))
                    .Add(x => x.ValueProperty, nameof(NullableItem.High))
                    .Add(x => x.Data, Data))
                .AddChildContent<RadzenStackedBarSeries<NullableItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(NullableItem.Name))
                    .Add(x => x.ValueProperty, nameof(NullableItem.Low))
                    .Add(x => x.Data, Data)));

            Assert.NotEmpty(chart.FindAll("path"));
        }

        [Fact]
        public async Task FullStackedBarSeries_TreatsNullAsZero()
        {
            using var ctx = CreateChartContext();

            var chart = await Render(ctx, p => p
                .AddChildContent<RadzenFullStackedBarSeries<NullableItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(NullableItem.Name))
                    .Add(x => x.ValueProperty, nameof(NullableItem.High))
                    .Add(x => x.Data, Data))
                .AddChildContent<RadzenFullStackedBarSeries<NullableItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(NullableItem.Name))
                    .Add(x => x.ValueProperty, nameof(NullableItem.Low))
                    .Add(x => x.Data, Data)));

            Assert.NotEmpty(chart.FindAll("path"));
        }

        [Fact]
        public async Task FullStackedColumnSeries_TreatsNullAsZero()
        {
            using var ctx = CreateChartContext();

            var chart = await Render(ctx, p => p
                .AddChildContent<RadzenFullStackedColumnSeries<NullableItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(NullableItem.Name))
                    .Add(x => x.ValueProperty, nameof(NullableItem.High))
                    .Add(x => x.Data, Data))
                .AddChildContent<RadzenFullStackedColumnSeries<NullableItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(NullableItem.Name))
                    .Add(x => x.ValueProperty, nameof(NullableItem.Low))
                    .Add(x => x.Data, Data)));

            Assert.NotEmpty(chart.FindAll("path"));
        }

        [Fact]
        public async Task StackedAreaSeries_TreatsNullAsZero()
        {
            using var ctx = CreateChartContext();

            var chart = await Render(ctx, p => p
                .AddChildContent<RadzenStackedAreaSeries<NullableItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(NullableItem.Name))
                    .Add(x => x.ValueProperty, nameof(NullableItem.High))
                    .Add(x => x.Data, Data))
                .AddChildContent<RadzenStackedAreaSeries<NullableItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(NullableItem.Name))
                    .Add(x => x.ValueProperty, nameof(NullableItem.Low))
                    .Add(x => x.Data, Data)));

            Assert.NotEmpty(chart.FindAll("path"));
        }

        [Fact]
        public async Task FullStackedAreaSeries_TreatsNullAsZero()
        {
            using var ctx = CreateChartContext();

            var chart = await Render(ctx, p => p
                .AddChildContent<RadzenFullStackedAreaSeries<NullableItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(NullableItem.Name))
                    .Add(x => x.ValueProperty, nameof(NullableItem.High))
                    .Add(x => x.Data, Data))
                .AddChildContent<RadzenFullStackedAreaSeries<NullableItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(NullableItem.Name))
                    .Add(x => x.ValueProperty, nameof(NullableItem.Low))
                    .Add(x => x.Data, Data)));

            Assert.NotEmpty(chart.FindAll("path"));
        }

        [Fact]
        public async Task StackedLineSeries_TreatsNullAsZero()
        {
            using var ctx = CreateChartContext();

            var chart = await Render(ctx, p => p
                .AddChildContent<RadzenStackedLineSeries<NullableItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(NullableItem.Name))
                    .Add(x => x.ValueProperty, nameof(NullableItem.High))
                    .Add(x => x.Data, Data))
                .AddChildContent<RadzenStackedLineSeries<NullableItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(NullableItem.Name))
                    .Add(x => x.ValueProperty, nameof(NullableItem.Low))
                    .Add(x => x.Data, Data)));

            Assert.NotEmpty(chart.FindAll("path"));
        }

        [Fact]
        public async Task FullStackedLineSeries_TreatsNullAsZero()
        {
            using var ctx = CreateChartContext();

            var chart = await Render(ctx, p => p
                .AddChildContent<RadzenFullStackedLineSeries<NullableItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(NullableItem.Name))
                    .Add(x => x.ValueProperty, nameof(NullableItem.High))
                    .Add(x => x.Data, Data))
                .AddChildContent<RadzenFullStackedLineSeries<NullableItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(NullableItem.Name))
                    .Add(x => x.ValueProperty, nameof(NullableItem.Low))
                    .Add(x => x.Data, Data)));

            Assert.NotEmpty(chart.FindAll("path"));
        }

        [Fact]
        public async Task BulletSeries_OmitsNullMeasure()
        {
            using var ctx = CreateChartContext();

            var chart = await Render(ctx, p => p.AddChildContent<RadzenBulletSeries<NullableItem>>(s => s
                .Add(x => x.CategoryProperty, nameof(NullableItem.Name))
                .Add(x => x.ValueProperty, nameof(NullableItem.Value))
                .Add(x => x.TargetProperty, nameof(NullableItem.Close))
                .Add(x => x.MaxProperty, nameof(NullableItem.Whole))
                .Add(x => x.Data, Data)));

            var paths = chart.FindAll("g.rz-bullet-series path").Count;

            using var full = CreateChartContext();
            var data = Data;

            foreach (var item in data)
            {
                item.Value ??= 1;
                item.Close ??= 1;
            }

            var complete = await Render(full, p => p.AddChildContent<RadzenBulletSeries<NullableItem>>(s => s
                .Add(x => x.CategoryProperty, nameof(NullableItem.Name))
                .Add(x => x.ValueProperty, nameof(NullableItem.Value))
                .Add(x => x.TargetProperty, nameof(NullableItem.Close))
                .Add(x => x.MaxProperty, nameof(NullableItem.Whole))
                .Add(x => x.Data, data)));

            Assert.Equal(complete.FindAll("g.rz-bullet-series path").Count - Empty * 2, paths);
        }

        [Fact]
        public async Task BubbleSeries_SkipsNullValues()
        {
            using var ctx = CreateChartContext();

            var chart = await Render(ctx, p => p.AddChildContent<RadzenBubbleSeries<NullableItem>>(s => s
                .Add(x => x.CategoryProperty, nameof(NullableItem.Whole))
                .Add(x => x.ValueProperty, nameof(NullableItem.Value))
                .Add(x => x.SizeProperty, nameof(NullableItem.High))
                .Add(x => x.Data, Data)));

            Assert.Equal(Total - Empty, chart.FindAll("g.rz-bubble-series circle").Count);
        }

        [Fact]
        public async Task HeatmapSeries_SkipsNullIntensity()
        {
            using var ctx = CreateChartContext();

            var chart = await Render(ctx, p => p.AddChildContent<RadzenHeatmapSeries<NullableItem>>(s => s
                .Add(x => x.CategoryProperty, nameof(NullableItem.Whole))
                .Add(x => x.ValueProperty, nameof(NullableItem.Whole))
                .Add(x => x.IntensityProperty, nameof(NullableItem.Value))
                .Add(x => x.Data, Data)));

            Assert.Equal(Total - Empty, chart.FindAll("g.rz-heatmap-series rect").Count);
        }

        [Fact]
        public async Task ContourSeries_AcceptsNullIntensity()
        {
            using var ctx = CreateChartContext();

            var data = Enumerable.Range(0, 16).Select(i => new NullableItem { Whole = i % 4, X = i / 4, Value = i == 5 ? null : i }).ToArray();

            var chart = await Render(ctx, p => p.AddChildContent<RadzenContourSeries<NullableItem>>(s => s
                .Add(x => x.CategoryProperty, nameof(NullableItem.Whole))
                .Add(x => x.ValueProperty, nameof(NullableItem.X))
                .Add(x => x.IntensityProperty, nameof(NullableItem.Value))
                .Add(x => x.Data, data)));

            Assert.Equal(3, chart.Instance.ValueScale.Input.End);
        }

        [Theory]
        [InlineData(typeof(RadzenPieSeries<NullableItem>))]
        [InlineData(typeof(RadzenDonutSeries<NullableItem>))]
        [InlineData(typeof(RadzenFunnelSeries<NullableItem>))]
        [InlineData(typeof(RadzenPyramidSeries<NullableItem>))]
        public async Task ProportionalSeries_TreatNullAsZero(Type seriesType)
        {
            using var ctx = CreateChartContext();

            var chart = await Render(ctx, p => p.AddChildContent(builder =>
            {
                builder.OpenComponent(0, seriesType);
                builder.AddAttribute(1, "CategoryProperty", nameof(NullableItem.Name));
                builder.AddAttribute(2, "ValueProperty", nameof(NullableItem.High));
                builder.AddAttribute(3, "Data", Data);
                builder.CloseComponent();
            }));

            Assert.NotEmpty(chart.FindAll("path"));
        }

        [Theory]
        [InlineData(nameof(NullableItem.NullableDate), nameof(NullableItem.Value))]
        [InlineData(nameof(NullableItem.X), nameof(NullableItem.Whole))]
        [InlineData(nameof(NullableItem.Name), nameof(NullableItem.Count))]
        public void RangeNavigatorLineSeries_LeavesGapForNulls(string category, string value)
        {
            using var ctx = CreateChartContext();

            var navigator = ctx.RenderComponent<RadzenRangeNavigator>(p => p
                .AddChildContent<RadzenRangeNavigatorLineSeries<NullableItem>>(s => s
                    .Add(x => x.CategoryProperty, category)
                    .Add(x => x.ValueProperty, value)
                    .Add(x => x.Data, Data)));

            Assert.DoesNotContain("NaN", navigator.Markup);
            Assert.NotEmpty(navigator.FindAll("g.rz-range-nav-series path"));
            Assert.True(double.IsFinite(navigator.Instance.ValueScale.Input.Start));
        }

        [Theory]
        [InlineData(typeof(RadzenSpiderSeries<NullableItem>))]
        [InlineData(typeof(RadzenSpiderColumnSeries<NullableItem>))]
        public void SpiderSeries_TreatNullAsZero(Type seriesType)
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenSpiderChart>(p => p.AddChildContent(builder =>
            {
                builder.OpenComponent(0, seriesType);
                builder.AddAttribute(1, "CategoryProperty", nameof(NullableItem.Name));
                builder.AddAttribute(2, "ValueProperty", nameof(NullableItem.Count));
                builder.AddAttribute(3, "Data", Data);
                builder.CloseComponent();
            }));

            Assert.Contains("rz-spider-chart", chart.Markup);
            Assert.DoesNotContain("NaN", chart.Markup);
        }
    }
}
