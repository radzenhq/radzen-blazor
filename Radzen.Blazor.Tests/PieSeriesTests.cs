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
        public async System.Threading.Tasks.Task PieSeries_CornerRadius_RoundsThreeCornersPerSlice()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenPieSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.CornerRadius, 10.0)
                    .Add(x => x.Data, SampleData)));

            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));

            // No sharp apex (no zero-radius inner arc) once corners are rounded.
            Assert.DoesNotContain("A 0 0", chart.Markup);
            // Three fillet arcs of radius 10 per slice (two outer corners + apex) across three slices.
            Assert.Equal(9, Count(chart.Markup, "A 10 10"));
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
