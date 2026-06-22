using System.Linq;
using System.Text.RegularExpressions;
using Bunit;
using Xunit;
using static Radzen.Blazor.Tests.ChartTestHelper;

namespace Radzen.Blazor.Tests
{
    public class PieSeriesTests
    {
        [Fact]
        public void PieSeries_Renders_ArcPathPerItem()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenPieSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Fills, new[] { "#FF0000", "#00FF00", "#0000FF" })
                    .Add(x => x.Data, SampleData)));

            Assert.Contains("rz-pie-series rz-series-0", chart.Markup);
            Assert.Contains("rz-series-item-0", chart.Markup);
            Assert.Contains("rz-series-item-1", chart.Markup);
            Assert.Contains("rz-series-item-2", chart.Markup);
        }

        [Fact]
        public void PieSeries_CustomFills_AppliedToArcPaths()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenPieSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Fills, new[] { "#FF0000", "#00FF00", "#0000FF" })
                    .Add(x => x.Data, SampleData)));

            Assert.Contains("fill: #FF0000", chart.Markup);
            Assert.Contains("fill: #00FF00", chart.Markup);
            Assert.Contains("fill: #0000FF", chart.Markup);
        }

        [Fact]
        public async System.Threading.Tasks.Task PieSeries_ArcPaths_ContainSvgArcCommands()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenPieSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Data, SampleData)));

            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));

            // Pie arcs use SVG A (arc) command with radius 118
            Assert.Contains("A 118 118", chart.Markup);
        }

        [Fact]
        public void PieSeries_Legend_ShowsCategoryNames()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenPieSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Data, SampleData)));

            // Pie legend shows category names, not series title
            Assert.Contains(">A</span>", chart.Markup);
            Assert.Contains(">B</span>", chart.Markup);
            Assert.Contains(">C</span>", chart.Markup);
        }

        [Fact]
        public void PieSeries_Hidden_NoArcPathsRendered()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenPieSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Visible, false)
                    .Add(x => x.Data, SampleData)));

            Assert.DoesNotContain("rz-pie-series", chart.Markup);
        }

        [Fact]
        public async System.Threading.Tasks.Task DonutSeries_Renders_DonutClass()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenDonutSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.InnerRadius, 50)
                    .Add(x => x.Data, SampleData)));

            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));

            Assert.Contains("rz-donut-series", chart.Markup);
            Assert.Contains("rz-series-item-0", chart.Markup);
            // Donut arcs have both outer and inner radius in the path
            Assert.Contains("A 118 118", chart.Markup); // outer
        }

        [Fact]
        public async System.Threading.Tasks.Task PieSeries_DefaultCornerRadius_UsesSharpPath()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenPieSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Data, SampleData)));

            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));

            // A sharp pie slice closes through the center with a zero-radius inner arc.
            Assert.Contains("A 0 0", chart.Markup);
        }

        [Fact]
        public async System.Threading.Tasks.Task PieSeries_CornerRadius_RoundsTwoOuterCornersPerSlice()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenPieSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.CornerRadius, 10.0)
                    .Add(x => x.Data, SampleData)));

            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));

            // The center apex stays sharp (no zero-radius inner arc, and not rounded either).
            Assert.DoesNotContain("A 0 0", chart.Markup);
            // Only the two outer corners are rounded per slice across three slices; the center is left sharp.
            Assert.Equal(6, Count(chart.Markup, "A 10 10"));
            Assert.DoesNotContain("NaN", chart.Markup);
        }

        [Fact]
        public async System.Threading.Tasks.Task DonutSeries_CornerRadius_RoundsFourCornersPerSlice()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenDonutSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.InnerRadius, 50)
                    .Add(x => x.CornerRadius, 10.0)
                    .Add(x => x.Data, SampleData)));

            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));

            // Main outer and inner arcs survive.
            Assert.Contains("A 118 118", chart.Markup);
            Assert.Contains("A 50 50", chart.Markup);
            // Four fillet arcs of radius 10 per slice across three slices.
            Assert.Equal(12, Count(chart.Markup, "A 10 10"));
            Assert.DoesNotContain("NaN", chart.Markup);
        }

        [Fact]
        public async System.Threading.Tasks.Task PieSeries_LargeCornerRadius_ClampedWithoutNaN()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenPieSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.CornerRadius, 1000.0)
                    .Add(x => x.Data, SampleData)));

            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));

            Assert.DoesNotContain("NaN", chart.Markup);
        }

        [Fact]
        public async System.Threading.Tasks.Task PieSeries_SingleItem_CornerRadiusFallsBackToSharpRing()
        {
            using var ctx = CreateChartContext();

            var single = new[] { new DataItem { Category = "A", Value = 10 } };

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenPieSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.CornerRadius, 10.0)
                    .Add(x => x.Data, single)));

            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));

            // A full-circle slice has no corners; it renders as the sharp ring (zero-radius inner arc).
            Assert.Contains("A 0 0", chart.Markup);
            Assert.DoesNotContain("A 10 10", chart.Markup);
        }

        [Fact]
        public async System.Threading.Tasks.Task PieSeries_DefaultSegmentGap_UsesSharpPath()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenPieSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Data, SampleData)));

            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));

            // No gap by default: slices still close through the center.
            Assert.Contains("A 0 0", chart.Markup);
        }

        [Fact]
        public async System.Threading.Tasks.Task PieSeries_SegmentGap_InsetsEdgesAndDropsCenterArc()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenPieSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.SegmentGap, 8.0)
                    .Add(x => x.Data, SampleData)));

            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));

            // Outer arcs survive; the inset edges meet at an apex rather than the center, so no zero-radius arc.
            Assert.Contains("A 118 118", chart.Markup);
            Assert.DoesNotContain("A 0 0", chart.Markup);
            Assert.DoesNotContain("NaN", chart.Markup);
        }

        [Fact]
        public async System.Threading.Tasks.Task PieSeries_SegmentGap_WithCornerRadius_RoundsWithoutNaN()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenPieSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.SegmentGap, 6.0)
                    .Add(x => x.CornerRadius, 8.0)
                    .Add(x => x.Data, SampleData)));

            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));

            Assert.DoesNotContain("A 0 0", chart.Markup);
            // Only the two outer corners are rounded per slice across three slices (sharp center apex).
            Assert.Equal(6, Count(chart.Markup, "A 8 8"));
            Assert.DoesNotContain("NaN", chart.Markup);
        }

        [Fact]
        public async System.Threading.Tasks.Task DonutSeries_SegmentGap_WithCornerRadius_RoundsWithoutNaN()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenDonutSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.InnerRadius, 50)
                    .Add(x => x.SegmentGap, 6.0)
                    .Add(x => x.CornerRadius, 8.0)
                    .Add(x => x.Data, SampleData)));

            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));

            Assert.Contains("A 118 118", chart.Markup);
            Assert.Contains("A 50 50", chart.Markup);
            // Four fillet arcs per slice across three slices.
            Assert.Equal(12, Count(chart.Markup, "A 8 8"));
            Assert.DoesNotContain("NaN", chart.Markup);
        }

        [Fact]
        public async System.Threading.Tasks.Task PieSeries_SegmentGap_TinySliceSkippedWithoutThrow()
        {
            using var ctx = CreateChartContext();

            var data = new[]
            {
                new DataItem { Category = "Tiny", Value = 0.2 },
                new DataItem { Category = "B", Value = 50 },
                new DataItem { Category = "C", Value = 50 },
            };

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenPieSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.SegmentGap, 40.0)
                    .Add(x => x.Data, data)));

            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));

            // The slice too narrow for the gap is skipped; the rest still render.
            Assert.Contains("A 118 118", chart.Markup);
            Assert.DoesNotContain("NaN", chart.Markup);
        }

        [Fact]
        public async System.Threading.Tasks.Task PieSeries_LargeSegmentGap_ClampedWithoutNaN()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenPieSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.SegmentGap, 1000.0)
                    .Add(x => x.Data, SampleData)));

            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));

            Assert.DoesNotContain("NaN", chart.Markup);
        }

        [Fact]
        public async System.Threading.Tasks.Task DonutSeries_SegmentGap_SmallerThanHole_KeepsInnerArc()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenDonutSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.InnerRadius, 50)
                    .Add(x => x.SegmentGap, 4.0)
                    .Add(x => x.Data, SampleData)));

            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));

            Assert.Contains("A 118 118", chart.Markup);
            Assert.Contains("A 50 50", chart.Markup);
            Assert.DoesNotContain("NaN", chart.Markup);
        }

        [Fact]
        public async System.Threading.Tasks.Task PieSeries_CornerRadius_CenterApexNotRounded_UnequalSlices()
        {
            using var ctx = CreateChartContext();

            var unequal = new[]
            {
                new DataItem { Category = "A", Value = 20 },
                new DataItem { Category = "B", Value = 50 },
                new DataItem { Category = "C", Value = 200 },
            };

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenPieSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.CornerRadius, 10.0)
                    .Add(x => x.Data, unequal)));

            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));

            // Apex stays sharp: two outer fillets per slice, no rounded or zero-radius center.
            Assert.Equal(6, Count(chart.Markup, "A 10 10"));
            Assert.DoesNotContain("A 0 0", chart.Markup);
            Assert.DoesNotContain("NaN", chart.Markup);

            // Every slice's inner edges run to the exact same center point, so the tips converge evenly
            // regardless of slice size. That shared "L cx cy" target appears once per slice (three times).
            var sharedTarget = Regex.Matches(chart.Markup, @"L (-?\d+(?:\.\d+)?) (-?\d+(?:\.\d+)?)")
                .Select(m => m.Value)
                .GroupBy(v => v)
                .Max(g => g.Count());

            Assert.Equal(3, sharedTarget);
        }

        class ExplodeItem
        {
            public string Category { get; set; } = "";
            public double Value { get; set; }
            public bool Exploded { get; set; }
        }

        static ExplodeItem[] ExplodeData => new[]
        {
            new ExplodeItem { Category = "A", Value = 10, Exploded = false },
            new ExplodeItem { Category = "B", Value = 20, Exploded = true },
            new ExplodeItem { Category = "C", Value = 15, Exploded = false },
        };

        [Fact]
        public void PieSeries_ExplodedProperty_MarksFlaggedSlices()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenPieSeries<ExplodeItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(ExplodeItem.Category))
                    .Add(x => x.ValueProperty, nameof(ExplodeItem.Value))
                    .Add(x => x.ExplodeOffset, 15.0)
                    .Add(x => x.ExplodedProperty, nameof(ExplodeItem.Exploded))
                    .Add(x => x.Data, ExplodeData)));

            // Only the single flagged slice carries the static-explode class, and the offset vars are emitted.
            Assert.Equal(1, Count(chart.Markup, "rz-state-exploded"));
            Assert.Contains("--rz-explode-x", chart.Markup);
        }

        [Fact]
        public void PieSeries_NoExplodedProperty_NoStaticExplodeClass()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenPieSeries<ExplodeItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(ExplodeItem.Category))
                    .Add(x => x.ValueProperty, nameof(ExplodeItem.Value))
                    .Add(x => x.ExplodeOffset, 15.0)
                    .Add(x => x.Data, ExplodeData)));

            Assert.DoesNotContain("rz-state-exploded", chart.Markup);
        }

        [Fact]
        public void PieSeries_ExplodedProperty_WithoutOffset_NoStaticExplodeClass()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenPieSeries<ExplodeItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(ExplodeItem.Category))
                    .Add(x => x.ValueProperty, nameof(ExplodeItem.Value))
                    .Add(x => x.ExplodedProperty, nameof(ExplodeItem.Exploded))
                    .Add(x => x.Data, ExplodeData)));

            // ExplodeOffset defaults to 0, which gates static explode off.
            Assert.DoesNotContain("rz-state-exploded", chart.Markup);
        }

        [Fact]
        public void DonutSeries_ExplodedProperty_MarksFlaggedSlices()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenDonutSeries<ExplodeItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(ExplodeItem.Category))
                    .Add(x => x.ValueProperty, nameof(ExplodeItem.Value))
                    .Add(x => x.ExplodeOffset, 15.0)
                    .Add(x => x.ExplodedProperty, nameof(ExplodeItem.Exploded))
                    .Add(x => x.Data, ExplodeData)));

            Assert.Equal(1, Count(chart.Markup, "rz-state-exploded"));
        }

        private static int Count(string haystack, string needle)
        {
            var count = 0;
            var index = 0;
            while ((index = haystack.IndexOf(needle, index, System.StringComparison.Ordinal)) >= 0)
            {
                count++;
                index += needle.Length;
            }
            return count;
        }
    }
}
