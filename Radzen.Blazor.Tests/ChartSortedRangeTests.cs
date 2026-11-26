using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Bunit;
using Xunit;
using static Radzen.Blazor.Tests.ChartTestHelper;

namespace Radzen.Blazor.Tests
{
    public class ChartSortedRangeTests
    {
        class Sample
        {
            public double X { get; set; }
            public DateTime Time { get; set; }
            public double Y { get; set; }
        }

        static Sample[] Wave(int count, double step = 1) => Enumerable.Range(0, count)
            .Select(i => new Sample { X = i * step, Time = new DateTime(2024, 1, 1).AddHours(i), Y = Math.Sin(i / 20.0) * 50 + i % 11 })
            .ToArray();

        static async Task<IRenderedComponent<RadzenChart>> Render(TestContext ctx, Sample[] data, double viewStart, double viewEnd, bool animate,
            string category = nameof(Sample.X), Interpolation interpolation = Interpolation.Line, bool inverted = false, bool area = false)
        {
            var chart = ctx.RenderComponent<RadzenChart>(p =>
            {
                p.Add(x => x.AllowZoom, true);
                p.Add(x => x.AnimateDataUpdates, animate);
                p.Add(x => x.ViewStart, viewStart);
                p.Add(x => x.ViewEnd, viewEnd);
                p.AddChildContent<RadzenCategoryAxis>(a => a.Add(x => x.Inverted, inverted));

                if (area)
                {
                    p.AddChildContent<RadzenAreaSeries<Sample>>(s => s
                        .Add(x => x.CategoryProperty, category)
                        .Add(x => x.ValueProperty, nameof(Sample.Y))
                        .Add(x => x.Interpolation, interpolation)
                        .Add(x => x.Data, data));
                }
                else
                {
                    p.AddChildContent<RadzenLineSeries<Sample>>(s => s
                        .Add(x => x.CategoryProperty, category)
                        .Add(x => x.ValueProperty, nameof(Sample.Y))
                        .Add(x => x.Interpolation, interpolation)
                        .Add(x => x.Data, data)
                        .AddChildContent<RadzenMarkers>(m => m.Add(x => x.Visible, false)));
                }
            });

            await chart.InvokeAsync(() => chart.Instance.Resize(700, 300));

            return chart;
        }

        static (double X, double Y)[] Points(string d) => Regex.Matches(d, @"(-?\d+(?:\.\d+)?(?:E[-+]?\d+)?) (-?\d+(?:\.\d+)?(?:E[-+]?\d+)?)")
            .Select(m => (double.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture), double.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture)))
            .ToArray();

        static (double X, double Y)[] LinePoints(IRenderedComponent<RadzenChart> chart, bool area = false) => area
            ? Points(chart.FindAll(".rz-area-series path").Last().GetAttribute("d")!)
            : Points(chart.Find(".rz-line-series path").GetAttribute("d")!);

        static (double X, double Y)[] Clip((double X, double Y)[] points, double width, int neighbors)
        {
            var keep = new bool[points.Length];

            for (var i = 0; i < points.Length; i++)
            {
                if (points[i].X >= 0 && points[i].X <= width)
                {
                    keep[i] = true;
                }

                if (i > 0 && Math.Max(points[i - 1].X, points[i].X) >= 0 && Math.Min(points[i - 1].X, points[i].X) <= width)
                {
                    keep[i - 1] = true;
                    keep[i] = true;
                }
            }

            var kept = Enumerable.Range(0, points.Length).Where(i => keep[i]).ToList();

            return points.Where((_, i) => keep[i] || kept.Any(k => Math.Abs(k - i) <= neighbors)).ToArray();
        }

        static async Task AssertSameAsClippingEveryPoint(Sample[] data, double viewStart, double viewEnd,
            string category = nameof(Sample.X), Interpolation interpolation = Interpolation.Line, bool inverted = false, bool area = false)
        {
            using var ctx = CreateChartContext();

            var all = await Render(ctx, data, viewStart, viewEnd, animate: true, category, interpolation, inverted, area);
            var clipped = await Render(ctx, data, viewStart, viewEnd, animate: false, category, interpolation, inverted, area);

            var width = clipped.Instance.CategoryScale.OutputSize;
            var expected = Clip(LinePoints(all, area), width, 1);
            var actual = LinePoints(clipped, area);

            Assert.Equal(expected.Length, actual.Length);

            for (var i = 0; i < expected.Length; i++)
            {
                Assert.Equal(expected[i].X, actual[i].X, 6);
                Assert.Equal(expected[i].Y, actual[i].Y, 6);
            }

            Assert.True(actual.Length < data.Length);
        }

        [Theory]
        [InlineData(0.0, 0.1)]
        [InlineData(0.37, 0.52)]
        [InlineData(0.9, 1.0)]
        [InlineData(0.5, 0.5004)]
        public Task LineSeries_NumericCategory_RendersTheSamePointsAsClippingEveryPoint(double viewStart, double viewEnd) =>
            AssertSameAsClippingEveryPoint(Wave(5000), viewStart, viewEnd);

        [Theory]
        [InlineData(0.0, 0.1)]
        [InlineData(0.37, 0.52)]
        [InlineData(0.9, 1.0)]
        public Task LineSeries_DateCategory_RendersTheSamePointsAsClippingEveryPoint(double viewStart, double viewEnd) =>
            AssertSameAsClippingEveryPoint(Wave(5000), viewStart, viewEnd, nameof(Sample.Time));

        [Theory]
        [InlineData(0.0, 0.1)]
        [InlineData(0.37, 0.52)]
        [InlineData(0.9, 1.0)]
        public Task LineSeries_InvertedAxis_RendersTheSamePointsAsClippingEveryPoint(double viewStart, double viewEnd) =>
            AssertSameAsClippingEveryPoint(Wave(5000), viewStart, viewEnd, inverted: true);

        [Theory]
        [InlineData(0.0, 0.1)]
        [InlineData(0.37, 0.52)]
        [InlineData(0.9, 1.0)]
        public Task AreaSeries_NumericCategory_RendersTheSamePointsAsClippingEveryPoint(double viewStart, double viewEnd) =>
            AssertSameAsClippingEveryPoint(Wave(5000), viewStart, viewEnd, area: true);

        [Fact]
        public Task LineSeries_ViewBetweenTwoPoints_RendersTheSegmentThatCrossesIt() =>
            AssertSameAsClippingEveryPoint(Wave(40, 100), 0.5001, 0.5002);

        [Fact]
        public Task LineSeries_RepeatedCategoryValues_RendersTheSamePointsAsClippingEveryPoint() =>
            AssertSameAsClippingEveryPoint(Wave(5000).Select((s, i) => new Sample { X = i / 5, Time = s.Time, Y = s.Y }).ToArray(), 0.37, 0.52);

        [Fact]
        public async Task LineSeries_SplineInterpolation_KeepsTwoPointsBeyondTheView()
        {
            using var ctx = CreateChartContext();

            var data = Wave(5000);
            var chart = await Render(ctx, data, 0.4, 0.5, animate: false, interpolation: Interpolation.Spline);
            var all = await Render(ctx, data, 0.4, 0.5, animate: true);

            var width = chart.Instance.CategoryScale.OutputSize;
            var inside = LinePoints(all).Select(p => p.X).Distinct().Count(x => x >= 0 && x <= width);
            var curves = Regex.Matches(chart.Find(".rz-line-series path").GetAttribute("d")!, "C").Count;

            Assert.Equal(inside + 2 * 3 - 1, curves);
        }

        static bool Inside((double X, double Y) point, (double X, double Y)[] polygon)
        {
            if (point.X < polygon.Min(p => p.X) || point.X > polygon.Max(p => p.X) || point.Y < polygon.Min(p => p.Y) || point.Y > polygon.Max(p => p.Y))
            {
                return false;
            }

            var inside = false;

            for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
            {
                if ((polygon[i].Y > point.Y) != (polygon[j].Y > point.Y) &&
                    point.X < (polygon[j].X - polygon[i].X) * (point.Y - polygon[i].Y) / (polygon[j].Y - polygon[i].Y) + polygon[i].X)
                {
                    inside = !inside;
                }
            }

            return inside;
        }

        static bool OnLine((double X, double Y)[] points, double x, double y, double tolerance)
        {
            for (var i = 0; i < points.Length - 1; i++)
            {
                var start = points[i];
                var end = points[i + 1];

                if (Inside((x, y), new[] { (start.X, start.Y - tolerance), (end.X, end.Y - tolerance), (end.X, end.Y + tolerance), (start.X, start.Y + tolerance) }))
                {
                    return true;
                }
            }

            return false;
        }

        [Theory]
        [InlineData(nameof(Sample.X), false)]
        [InlineData(nameof(Sample.Time), false)]
        [InlineData(nameof(Sample.X), true)]
        public async Task LineSeries_Contains_GivesTheSameAnswerAsTestingEverySegment(string category, bool inverted)
        {
            using var ctx = CreateChartContext();

            var data = Wave(600);
            var chart = await Render(ctx, data, 0, 1, animate: true, category, inverted: inverted);
            var series = chart.FindComponent<RadzenLineSeries<Sample>>().Instance;
            var points = LinePoints(chart);
            var hits = 0;

            for (var index = 0; index < points.Length - 1; index += 7)
            {
                var middle = (X: (points[index].X + points[index + 1].X) / 2, Y: (points[index].Y + points[index + 1].Y) / 2);

                foreach (var probe in new[] { middle, (middle.X, middle.Y + 2), (middle.X, middle.Y + 40), (middle.X, middle.Y - 40), points[index], (points[index].X + 0.25, points[index].Y - 1) })
                {
                    var expected = OnLine(points, probe.Item1, probe.Item2, 3);

                    Assert.Equal(expected, series.Contains(probe.Item1, probe.Item2, 3));

                    hits += expected ? 1 : 0;
                }
            }

            Assert.True(hits > 100);
            Assert.False(series.Contains(points.Min(p => p.X) - 10, points[0].Y, 3));
            Assert.False(series.Contains(points.Max(p => p.X) + 10, points[^1].Y, 3));
        }
    }
}
