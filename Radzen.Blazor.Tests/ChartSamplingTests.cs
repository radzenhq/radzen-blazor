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
    public class ChartSamplingTests
    {
        class Sample
        {
            public double X { get; set; }
            public double? Y { get; set; }
        }

        const int Width = 600;

        static Sample[] Wave(int count) => Enumerable.Range(0, count)
            .Select(i => new Sample { X = i, Y = Math.Sin(i / 50.0) * 100 + (i * 7919 % 13) })
            .ToArray();

        static async Task<IRenderedComponent<RadzenChart>> RenderLine(TestContext ctx, Sample[] data, Sampling sampling, bool animate = false, bool markers = false)
        {
            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .Add(x => x.AnimateDataUpdates, animate)
                .AddChildContent<RadzenLineSeries<Sample>>(s => s
                    .Add(x => x.CategoryProperty, nameof(Sample.X))
                    .Add(x => x.ValueProperty, nameof(Sample.Y))
                    .Add(x => x.Sampling, sampling)
                    .Add(x => x.Data, data)
                    .AddChildContent<RadzenMarkers>(m => m.Add(x => x.Visible, markers))));

            await chart.InvokeAsync(() => chart.Instance.Resize(Width, 300));

            return chart;
        }

        static async Task<IRenderedComponent<RadzenChart>> RenderArea(TestContext ctx, Sample[] data, Sampling sampling)
        {
            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenAreaSeries<Sample>>(s => s
                    .Add(x => x.CategoryProperty, nameof(Sample.X))
                    .Add(x => x.ValueProperty, nameof(Sample.Y))
                    .Add(x => x.Sampling, sampling)
                    .Add(x => x.Data, data)));

            await chart.InvokeAsync(() => chart.Instance.Resize(Width, 300));

            return chart;
        }

        static (double X, double Y)[] Points(string d) => Regex.Matches(d, @"(-?\d+(?:\.\d+)?(?:E-?\d+)?) (-?\d+(?:\.\d+)?(?:E-?\d+)?)")
            .Select(m => (double.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture), double.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture)))
            .ToArray();

        static (double X, double Y)[] LinePoints(IRenderedComponent<RadzenChart> chart) =>
            Points(chart.Find(".rz-line-series path").GetAttribute("d")!);

        [Fact]
        public async Task LineSeries_WithoutSampling_RendersEveryPoint()
        {
            using var ctx = CreateChartContext();

            var chart = await RenderLine(ctx, Wave(20000), Sampling.None);

            Assert.Equal(20000, LinePoints(chart).Length);
        }

        [Fact]
        public async Task LineSeries_MinMaxSampling_RendersAtMostEightPointsPerPixel()
        {
            using var ctx = CreateChartContext();

            var chart = await RenderLine(ctx, Wave(100000), Sampling.MinMax);

            var points = LinePoints(chart);

            Assert.InRange(points.Length, 2, Width * 8);
            Assert.All(points.GroupBy(p => Math.Floor(p.X * 2)), bucket => Assert.InRange(bucket.Count(), 1, 4));
        }

        [Fact]
        public async Task LineSeries_MinMaxSampling_KeepsTheExtremesOfEveryPixel()
        {
            using var ctx = CreateChartContext();

            var data = Wave(20000);
            var all = LinePoints(await RenderLine(ctx, data, Sampling.None));
            var sampled = LinePoints(await RenderLine(ctx, data, Sampling.MinMax));

            var expected = all.GroupBy(p => Math.Floor(p.X)).ToDictionary(g => g.Key, g => (Min: g.Min(p => p.Y), Max: g.Max(p => p.Y)));
            var actual = sampled.GroupBy(p => Math.Floor(p.X)).ToDictionary(g => g.Key, g => (Min: g.Min(p => p.Y), Max: g.Max(p => p.Y)));

            Assert.True(sampled.Length < all.Length);
            Assert.Equal(expected.Count, actual.Count);

            foreach (var pixel in expected)
            {
                Assert.Equal(pixel.Value.Min, actual[pixel.Key].Min, 6);
                Assert.Equal(pixel.Value.Max, actual[pixel.Key].Max, 6);
            }

            Assert.Equal(all[0], sampled[0]);
            Assert.Equal(all[^1], sampled[^1]);
        }

        [Fact]
        public async Task LineSeries_MinMaxSampling_KeepsPointsInSeriesOrder()
        {
            using var ctx = CreateChartContext();

            var sampled = LinePoints(await RenderLine(ctx, Wave(20000), Sampling.MinMax));

            Assert.Equal(sampled.OrderBy(p => p.X).Select(p => p.X), sampled.Select(p => p.X));
        }

        [Fact]
        public async Task LineSeries_MinMaxSampling_LeavesSmallSeriesUnchanged()
        {
            using var ctx = CreateChartContext();

            var data = Wave(200);
            var all = await RenderLine(ctx, data, Sampling.None);
            var sampled = await RenderLine(ctx, data, Sampling.MinMax);

            Assert.Equal(all.Find(".rz-line-series path").GetAttribute("d"), sampled.Find(".rz-line-series path").GetAttribute("d"));
        }

        [Fact]
        public async Task LineSeries_MinMaxSampling_KeepsGaps()
        {
            using var ctx = CreateChartContext();

            var data = Wave(20000);
            data[5000].Y = null;
            data[15000].Y = null;

            var all = await RenderLine(ctx, data, Sampling.None);
            var sampled = await RenderLine(ctx, data, Sampling.MinMax);

            var allMoves = Regex.Matches(all.Find(".rz-line-series path").GetAttribute("d")!, "M").Count;
            var sampledMoves = Regex.Matches(sampled.Find(".rz-line-series path").GetAttribute("d")!, "M").Count;

            Assert.Equal(3, allMoves);
            Assert.Equal(allMoves, sampledMoves);
        }

        [Fact]
        public async Task LineSeries_MinMaxSampling_IsSkippedWhenAnimatingDataUpdates()
        {
            using var ctx = CreateChartContext();

            var chart = await RenderLine(ctx, Wave(20000), Sampling.MinMax, animate: true);

            Assert.Equal(20000, LinePoints(chart).Length);
        }

        [Fact]
        public async Task LineSeries_MinMaxSampling_RendersMarkersForSampledPointsOnly()
        {
            using var ctx = CreateChartContext();

            var chart = await RenderLine(ctx, Wave(20000), Sampling.MinMax, markers: true);

            Assert.Equal(LinePoints(chart).Length, chart.FindAll(".rz-line-series .rz-marker").Count);
        }

        [Fact]
        public async Task AreaSeries_MinMaxSampling_RendersFewerPointsWithTheSameExtremes()
        {
            using var ctx = CreateChartContext();

            var data = Wave(20000);
            var all = await RenderArea(ctx, data, Sampling.None);
            var sampled = await RenderArea(ctx, data, Sampling.MinMax);

            var allLine = Points(all.FindAll(".rz-area-series path").Last().GetAttribute("d")!);
            var sampledLine = Points(sampled.FindAll(".rz-area-series path").Last().GetAttribute("d")!);

            Assert.InRange(sampledLine.Length, 2, Width * 8);
            Assert.True(sampledLine.Length < allLine.Length);
            Assert.Equal(allLine.Min(p => p.Y), sampledLine.Min(p => p.Y), 6);
            Assert.Equal(allLine.Max(p => p.Y), sampledLine.Max(p => p.Y), 6);
        }

        static async Task<IRenderedComponent<RadzenChart>> RenderZoomable(TestContext ctx, Sample[] data, Sampling sampling, double viewStart, double viewEnd, bool inverted = false)
        {
            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .Add(x => x.AllowZoom, true)
                .Add(x => x.ViewStart, viewStart)
                .Add(x => x.ViewEnd, viewEnd)
                .AddChildContent<RadzenCategoryAxis>(a => a.Add(x => x.Inverted, inverted))
                .AddChildContent<RadzenLineSeries<Sample>>(s => s
                    .Add(x => x.CategoryProperty, nameof(Sample.X))
                    .Add(x => x.ValueProperty, nameof(Sample.Y))
                    .Add(x => x.Sampling, sampling)
                    .Add(x => x.Data, data)));

            await chart.InvokeAsync(() => chart.Instance.Resize(Width, 300));

            return chart;
        }

        [Theory]
        [InlineData(0.0, 1.0, false, 1)]
        [InlineData(0.3, 0.45, false, 1)]
        [InlineData(0.3, 0.45, true, 1)]
        [InlineData(0.0, 1.0, false, 4)]
        [InlineData(0.62, 0.63, false, 4)]
        [InlineData(0.5, 0.5002, false, 1)]
        public async Task LineSeries_MinMaxSampling_FindsTheTooltipItemUnderTheCursor(double viewStart, double viewEnd, bool inverted, int repeat)
        {
            using var ctx = CreateChartContext();

            var data = Wave(6000);

            foreach (var item in data)
            {
                item.X = Math.Floor(item.X / repeat);
            }

            var plainChart = await RenderZoomable(ctx, data, Sampling.None, viewStart, viewEnd, inverted);
            var plain = plainChart.FindComponent<RadzenLineSeries<Sample>>().Instance;
            var sampled = (await RenderZoomable(ctx, data, Sampling.MinMax, viewStart, viewEnd, inverted)).FindComponent<RadzenLineSeries<Sample>>().Instance;
            var plotWidth = plainChart.Instance.CategoryScale.OutputSize;
            var zoomed = plainChart.Instance.CategoryScale.IsZoomed;
            var positions = data.Select(item => (Item: item, X: plain.TooltipX(item), Y: plain.TooltipY(item))).ToArray();
            var underCursor = 0;

            for (var x = -40.0; x <= Width + 40; x += 7.3)
            {
                foreach (var y in new[] { 0.0, 140.0, 300.0 })
                {
                    var expected = plain.DataAt(x, y);
                    var actual = sampled.DataAt(x, y);
                    var column = positions.Where(p => Math.Abs(p.X - x) <= 0.5 && (!zoomed || (p.X >= 0 && p.X <= plotWidth))).ToArray();

                    if (expected.Item1 == null || column.Length == 0)
                    {
                        Assert.Same(expected.Item1, actual.Item1);
                        continue;
                    }

                    var nearest = column.Min(p => Math.Abs(p.Y - y));
                    var chosen = positions.First(p => ReferenceEquals(p.Item, actual.Item1));

                    Assert.Contains(column, p => ReferenceEquals(p.Item, actual.Item1));
                    Assert.Equal(nearest, Math.Abs(chosen.Y - y), 6);

                    underCursor++;
                }
            }

            Assert.True(underCursor > 0 || viewEnd - viewStart < 0.01);
        }

        [Theory]
        [InlineData(0.0, 1.0)]
        [InlineData(0.3, 0.45)]
        public async Task LineSeries_MinMaxSampling_HitTestsTheSameAsWithoutSampling(double viewStart, double viewEnd)
        {
            using var ctx = CreateChartContext();

            var data = Wave(1500);
            var plain = (await RenderZoomable(ctx, data, Sampling.None, viewStart, viewEnd)).FindComponent<RadzenLineSeries<Sample>>().Instance;
            var sampled = (await RenderZoomable(ctx, data, Sampling.MinMax, viewStart, viewEnd)).FindComponent<RadzenLineSeries<Sample>>().Instance;
            var hits = 0;

            for (var x = 0.0; x <= Width; x += 17.3)
            {
                for (var y = 0.0; y <= 300; y += 5)
                {
                    var expected = plain.Contains(x, y, 4);

                    Assert.Equal(expected, sampled.Contains(x, y, 4));

                    hits += expected ? 1 : 0;
                }
            }

            Assert.True(hits > 20);
        }

        static (double Min, double Max) ValueRange(IRenderedComponent<RadzenChart> chart) =>
            (chart.Instance.ValueScale.Input.Start, chart.Instance.ValueScale.Input.End);

        [Theory]
        [InlineData(Sampling.None)]
        [InlineData(Sampling.MinMax)]
        public async Task LineSeries_ReadsNewDataWhenTheCollectionIsReplaced(Sampling sampling)
        {
            using var ctx = CreateChartContext();

            var current = Wave(6000);

            Microsoft.AspNetCore.Components.RenderFragment content = builder =>
            {
                builder.OpenComponent<RadzenLineSeries<Sample>>(0);
                builder.AddAttribute(1, nameof(RadzenLineSeries<Sample>.CategoryProperty), nameof(Sample.X));
                builder.AddAttribute(2, nameof(RadzenLineSeries<Sample>.ValueProperty), nameof(Sample.Y));
                builder.AddAttribute(3, nameof(RadzenLineSeries<Sample>.Sampling), sampling);
                builder.AddAttribute(4, nameof(RadzenLineSeries<Sample>.Data), current);
                builder.CloseComponent();
            };

            var chart = ctx.RenderComponent<RadzenChart>(p => p.Add(x => x.ChildContent, content));

            await chart.InvokeAsync(() => chart.Instance.Resize(Width, 300));

            var before = ValueRange(chart);

            current = Wave(6000);

            foreach (var item in current)
            {
                item.Y += 5000;
            }

            chart.SetParametersAndRender(p => p.Add(x => x.ChildContent, content));

            var after = ValueRange(chart);

            Assert.True(after.Min > before.Max);
        }

        [Fact]
        public async Task LineSeries_MinMaxSampling_ReadsDataChangedInPlaceAfterReload()
        {
            using var ctx = CreateChartContext();

            var data = Wave(6000);
            var chart = await RenderZoomable(ctx, data, Sampling.MinMax, 0, 1);
            var before = ValueRange(chart);

            foreach (var item in data)
            {
                item.Y += 5000;
            }

            await chart.InvokeAsync(() => chart.Instance.Reload());

            var after = ValueRange(chart);

            Assert.True(after.Min > before.Max);
        }

        [Fact]
        public async Task LineSeries_WithoutSampling_ReadsDataChangedInPlaceOnTheNextLayout()
        {
            using var ctx = CreateChartContext();

            var data = Wave(6000);
            var chart = await RenderZoomable(ctx, data, Sampling.None, 0, 1);
            var before = ValueRange(chart);

            foreach (var item in data)
            {
                item.Y += 5000;
            }

            await chart.InvokeAsync(() => chart.Instance.Resize(Width + 10, 300));

            var after = ValueRange(chart);

            Assert.True(after.Min > before.Max);
        }
    }
}
