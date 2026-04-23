using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Bunit;
using Xunit;
using static Radzen.Blazor.Tests.ChartTestHelper;

namespace Radzen.Blazor.Tests
{
    public class ContourSeriesTests
    {
        private class ContourItem
        {
            public double X { get; set; }
            public double Y { get; set; }
            public double Value { get; set; }
        }

        // A 3x3 grid with a radial gradient centred at (1,1)
        private static ContourItem[] Grid3x3
        {
            get
            {
                var items = new List<ContourItem>();
                for (var ix = 0; ix < 3; ix++)
                {
                    for (var iy = 0; iy < 3; iy++)
                    {
                        var dx = ix - 1;
                        var dy = iy - 1;
                        items.Add(new ContourItem { X = ix, Y = iy, Value = 10 - (dx * dx + dy * dy) });
                    }
                }
                return items.ToArray();
            }
        }

        private static IList<SeriesColorRange> Ranges => new[]
        {
            new SeriesColorRange { Min = 0, Max = 7, Color = "#0000ff" },
            new SeriesColorRange { Min = 7, Max = 9, Color = "#00ff00" },
            new SeriesColorRange { Min = 9, Max = 100, Color = "#ff0000" },
        };

        [Fact]
        public async System.Threading.Tasks.Task ContourSeries_Renders_FilledPolygons()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenContourSeries<ContourItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(ContourItem.X))
                    .Add(x => x.ValueProperty, nameof(ContourItem.Y))
                    .Add(x => x.IntensityProperty, nameof(ContourItem.Value))
                    .Add(x => x.ColorRange, Ranges)
                    .Add(x => x.Data, Grid3x3)));

            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));

            Assert.Contains("rz-contour-series rz-series-0", chart.Markup);
            var polyCount = Regex.Matches(chart.Markup, "<polygon ").Count;
            Assert.True(polyCount > 0, $"Expected filled polygons, got {polyCount}");
            // All three band colors should appear somewhere
            Assert.Contains("#0000ff", chart.Markup);
            Assert.Contains("#00ff00", chart.Markup);
            Assert.Contains("#ff0000", chart.Markup);
        }

        [Fact]
        public async System.Threading.Tasks.Task ContourSeries_ShowLines_EmitsLineElements()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenContourSeries<ContourItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(ContourItem.X))
                    .Add(x => x.ValueProperty, nameof(ContourItem.Y))
                    .Add(x => x.IntensityProperty, nameof(ContourItem.Value))
                    .Add(x => x.ColorRange, Ranges)
                    .Add(x => x.ShowLines, true)
                    .Add(x => x.LineColor, "#000")
                    .Add(x => x.Data, Grid3x3)));

            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));

            var lineCount = Regex.Matches(chart.Markup, "<line ").Count;
            Assert.True(lineCount > 0, $"Expected iso-lines when ShowLines=true, got {lineCount}");
        }

        [Fact]
        public void ContourSeries_ClipLow_RemovesVerticesBelowThreshold()
        {
            var triangle = new List<RadzenContourSeries<ContourItem>.Vertex>
            {
                new(0, 0, 0),
                new(1, 0, 10),
                new(0, 1, 5),
            };

            var clipped = RadzenContourSeries<ContourItem>.ClipLow(triangle, 5);

            // The vertex at (0,0,0) is below 5 and gets removed; two crossings are added.
            Assert.True(clipped.Count >= 3);
            Assert.All(clipped, v => Assert.True(v.V >= 5 - 1e-9));
        }

        [Fact]
        public void ContourSeries_ClipHigh_RemovesVerticesAboveThreshold()
        {
            var triangle = new List<RadzenContourSeries<ContourItem>.Vertex>
            {
                new(0, 0, 0),
                new(1, 0, 10),
                new(0, 1, 5),
            };

            var clipped = RadzenContourSeries<ContourItem>.ClipHigh(triangle, 5);

            Assert.True(clipped.Count >= 3);
            Assert.All(clipped, v => Assert.True(v.V <= 5 + 1e-9));
        }

        [Fact]
        public async System.Threading.Tasks.Task ContourSeries_Legend_ShowsEntryPerColorRange()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenContourSeries<ContourItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(ContourItem.X))
                    .Add(x => x.ValueProperty, nameof(ContourItem.Y))
                    .Add(x => x.IntensityProperty, nameof(ContourItem.Value))
                    .Add(x => x.ColorRange, Ranges)
                    .Add(x => x.Data, Grid3x3)));

            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));

            var legendItems = Regex.Matches(chart.Markup, "rz-legend-item\"").Count;
            Assert.True(legendItems >= Ranges.Count, $"Expected >= {Ranges.Count} legend items, got {legendItems}");
        }
    }
}
