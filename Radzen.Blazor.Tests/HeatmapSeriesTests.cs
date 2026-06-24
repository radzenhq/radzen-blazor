using System.Collections.Generic;
using System.Text.RegularExpressions;
using Bunit;
using Xunit;
using static Radzen.Blazor.Tests.ChartTestHelper;

namespace Radzen.Blazor.Tests
{
    public class HeatmapSeriesTests
    {
        private class HeatItem
        {
            public double X { get; set; }
            public double Y { get; set; }
            public double Lux { get; set; }
        }

        private static HeatItem[] Grid2x2 => new[]
        {
            new HeatItem { X = 0, Y = 0, Lux = 1 },
            new HeatItem { X = 1, Y = 0, Lux = 3 },
            new HeatItem { X = 0, Y = 1, Lux = 5 },
            new HeatItem { X = 1, Y = 1, Lux = 7 },
        };

        private static IList<SeriesColorRange> Ranges => new[]
        {
            new SeriesColorRange { Min = 0, Max = 2.5, Color = "#0000ff" },
            new SeriesColorRange { Min = 2.5, Max = 5, Color = "#00ff00" },
            new SeriesColorRange { Min = 5, Max = 10, Color = "#ff0000" },
        };

        [Fact]
        public async System.Threading.Tasks.Task HeatmapSeries_Renders_RectPerCell()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenHeatmapSeries<HeatItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(HeatItem.X))
                    .Add(x => x.ValueProperty, nameof(HeatItem.Y))
                    .Add(x => x.IntensityProperty, nameof(HeatItem.Lux))
                    .Add(x => x.ColorRange, Ranges)
                    .Add(x => x.Data, Grid2x2)));

            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));

            Assert.Contains("rz-heatmap-series rz-series-0", chart.Markup);
            var rectCount = Regex.Matches(chart.Markup, "<rect ").Count;
            Assert.True(rectCount >= 4, $"Expected at least 4 cell rects, got {rectCount}");
        }

        [Fact]
        public async System.Threading.Tasks.Task HeatmapSeries_Uses_ColorRange_ForFill()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenHeatmapSeries<HeatItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(HeatItem.X))
                    .Add(x => x.ValueProperty, nameof(HeatItem.Y))
                    .Add(x => x.IntensityProperty, nameof(HeatItem.Lux))
                    .Add(x => x.ColorRange, Ranges)
                    .Add(x => x.Data, Grid2x2)));

            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));

            // Lux=1 → blue, Lux=3 → green, Lux=5 or 7 → red
            Assert.Contains("#0000ff", chart.Markup);
            Assert.Contains("#00ff00", chart.Markup);
            Assert.Contains("#ff0000", chart.Markup);
        }

        [Fact]
        public async System.Threading.Tasks.Task HeatmapSeries_Legend_ShowsOneItemPerColorRange()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenHeatmapSeries<HeatItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(HeatItem.X))
                    .Add(x => x.ValueProperty, nameof(HeatItem.Y))
                    .Add(x => x.IntensityProperty, nameof(HeatItem.Lux))
                    .Add(x => x.ColorRange, Ranges)
                    .Add(x => x.Data, Grid2x2)));

            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));

            var legendItems = Regex.Matches(chart.Markup, "rz-legend-item\"").Count;
            Assert.True(legendItems >= Ranges.Count, $"Expected >= {Ranges.Count} legend items, got {legendItems}");
        }

        [Fact]
        public void HeatmapSeries_Hidden_NoRectsRendered()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenHeatmapSeries<HeatItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(HeatItem.X))
                    .Add(x => x.ValueProperty, nameof(HeatItem.Y))
                    .Add(x => x.IntensityProperty, nameof(HeatItem.Lux))
                    .Add(x => x.ColorRange, Ranges)
                    .Add(x => x.Visible, false)
                    .Add(x => x.Data, Grid2x2)));

            Assert.DoesNotContain("rz-heatmap-series", chart.Markup);
        }
    }
}
