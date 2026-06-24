using System.Linq;
using System.Text.RegularExpressions;
using Bunit;
using Xunit;
using static Radzen.Blazor.Tests.ChartTestHelper;

namespace Radzen.Blazor.Tests
{
    public class DataLabelTests
    {
        static IRenderedComponent<RadzenChart> RenderChart(TestContext ctx,
            System.Action<ComponentParameterCollectionBuilder<RadzenSeriesDataLabels>>? labels = null,
            System.Action<ComponentParameterCollectionBuilder<RadzenSeriesDataLabels>>? secondSeriesLabels = null)
        {
            return ctx.RenderComponent<RadzenChart>(p =>
            {
                p.AddChildContent<RadzenLineSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Data, SampleData)
                    .AddChildContent<RadzenSeriesDataLabels>(d => labels?.Invoke(d)));

                if (secondSeriesLabels != null)
                {
                    p.AddChildContent<RadzenLineSeries<DataItem>>(s => s
                        .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                        .Add(x => x.ValueProperty, nameof(DataItem.Value))
                        .Add(x => x.Data, SampleData)
                        .AddChildContent<RadzenSeriesDataLabels>(d => secondSeriesLabels.Invoke(d)));
                }
            });
        }

        static int LabelGroups(string markup) => Regex.Matches(markup, "rz-series-data-label-group").Count;
        static int Chips(string markup) => Regex.Matches(markup, "rz-series-data-label-chip").Count;
        static string[] LabelTexts(string markup) =>
            Regex.Matches(markup, "class=\"rz-series-data-label\"[^>]*>([^<]+)<").Select(m => m.Groups[1].Value).ToArray();

        [Fact]
        public async System.Threading.Tasks.Task DataLabels_RenderBackgroundChip_ByDefault()
        {
            using var ctx = CreateChartContext();
            var chart = RenderChart(ctx);
            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));

            Assert.Equal(3, LabelGroups(chart.Markup));
            Assert.Equal(3, Chips(chart.Markup));
        }

        [Fact]
        public async System.Threading.Tasks.Task DataLabels_PlainAppearance_RendersPlainText()
        {
            using var ctx = CreateChartContext();
            var chart = RenderChart(ctx, d => d.Add(x => x.Appearance, DataLabelAppearance.Plain));
            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));

            Assert.Equal(3, LabelGroups(chart.Markup));
            Assert.Equal(0, Chips(chart.Markup));
        }

        [Fact]
        public async System.Threading.Tasks.Task DataLabels_OutlineAppearance_AddsHaloClass()
        {
            using var ctx = CreateChartContext();
            var chart = RenderChart(ctx, d => d.Add(x => x.Appearance, DataLabelAppearance.Outline));
            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));

            Assert.Equal(0, Chips(chart.Markup));
            Assert.Contains("rz-series-data-label-outline", chart.Markup);
        }

        [Fact]
        public async System.Threading.Tasks.Task DataLabels_RenderAboveAllSeries()
        {
            using var ctx = CreateChartContext();
            var chart = RenderChart(ctx, null, d => { });
            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));

            // The first series' labels must appear after the LAST series group in document order
            // (SVG z-order), so no series line can draw over a label.
            var lastSeries = chart.Markup.LastIndexOf("rz-line-series", System.StringComparison.Ordinal);
            var firstLabel = chart.Markup.IndexOf("rz-series-data-label-group", System.StringComparison.Ordinal);

            Assert.True(firstLabel > lastSeries, $"data label at {firstLabel} should render after the last series at {lastSeries}");
        }

        [Fact]
        public async System.Threading.Tasks.Task DataLabels_OverlappingSeries_HiddenByDefault()
        {
            using var ctx = CreateChartContext();
            // Two identical series - labels land at identical positions, so the second series' labels hide.
            var chart = RenderChart(ctx, null, d => { });
            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));

            Assert.Equal(3, LabelGroups(chart.Markup));
        }

        [Fact]
        public async System.Threading.Tasks.Task DataLabels_AllowOverlap_RendersAll()
        {
            using var ctx = CreateChartContext();
            var chart = RenderChart(ctx,
                d => d.Add(x => x.AllowOverlap, true),
                d => d.Add(x => x.AllowOverlap, true));
            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));

            Assert.Equal(6, LabelGroups(chart.Markup));
        }

        [Fact]
        public async System.Threading.Tasks.Task DataLabels_FormatString_OverridesAxisFormat()
        {
            using var ctx = CreateChartContext();
            var chart = RenderChart(ctx, d => d.Add(x => x.FormatString, "{0:0}!"));
            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));

            Assert.Contains("20!", chart.Markup);
        }

        [Fact]
        public async System.Threading.Tasks.Task DataLabels_Formatter_TakesPrecedence()
        {
            using var ctx = CreateChartContext();
            var chart = RenderChart(ctx, d => d
                .Add(x => x.FormatString, "{0:0}!")
                .Add(x => x.Formatter, value => $"[{value}]"));
            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));

            Assert.Contains("[20]", chart.Markup);
            Assert.DoesNotContain("20!", chart.Markup);
        }

        [Fact]
        public async System.Threading.Tasks.Task DataLabels_Step_SkipsLabels()
        {
            using var ctx = CreateChartContext();
            var chart = RenderChart(ctx, d => d.Add(x => x.Step, 2));
            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));

            // 3 points, step 2 - indexes 0 and 2.
            Assert.Equal(2, LabelGroups(chart.Markup));
        }

        [Fact]
        public async System.Threading.Tasks.Task DataLabels_DisplayMinMax_ShowsOnlyExtremes()
        {
            using var ctx = CreateChartContext();
            // SampleData values: A=10, B=20, C=15.
            var chart = RenderChart(ctx, d => d.Add(x => x.Display, DataLabelDisplay.MinMax));
            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));

            Assert.Equal(2, LabelGroups(chart.Markup));
            Assert.Equal(new[] { "10", "20" }, LabelTexts(chart.Markup).OrderBy(t => t).ToArray());
        }

        [Fact]
        public async System.Threading.Tasks.Task DataLabels_DisplayFirstLast_ShowsEnds()
        {
            using var ctx = CreateChartContext();
            var chart = RenderChart(ctx, d => d.Add(x => x.Display, DataLabelDisplay.FirstLast));
            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));

            Assert.Equal(2, LabelGroups(chart.Markup));
            Assert.Equal(new[] { "10", "15" }, LabelTexts(chart.Markup).OrderBy(t => t).ToArray());
        }

        static IRenderedComponent<RadzenChart> RenderSeriesChart<TSeries>(TestContext ctx,
            System.Action<ComponentParameterCollectionBuilder<RadzenSeriesDataLabels>>? labels = null,
            DataItem[]? data = null)
            where TSeries : Radzen.Blazor.CartesianSeries<DataItem>
        {
            return ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<TSeries>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Data, data ?? SampleData)
                    .AddChildContent<RadzenSeriesDataLabels>(d => labels?.Invoke(d))));
        }

        [Fact]
        public async System.Threading.Tasks.Task AreaSeries_DataLabels_RenderChips_AndFormat()
        {
            using var ctx = CreateChartContext();
            var chart = RenderSeriesChart<RadzenAreaSeries<DataItem>>(ctx, d => d.Add(x => x.FormatString, "{0:0}u"));
            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));

            Assert.Equal(3, LabelGroups(chart.Markup));
            Assert.Equal(3, Chips(chart.Markup));
            Assert.Contains("20u", chart.Markup);
        }

        [Fact]
        public async System.Threading.Tasks.Task StackedAreaSeries_DataLabels_MinMax_UsesRawValues()
        {
            using var ctx = CreateChartContext();
            var chart = RenderSeriesChart<RadzenStackedAreaSeries<DataItem>>(ctx, d => d.Add(x => x.Display, DataLabelDisplay.MinMax));
            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));

            Assert.Equal(2, LabelGroups(chart.Markup));
            Assert.Equal(new[] { "10", "20" }, LabelTexts(chart.Markup).OrderBy(t => t).ToArray());
        }

        [Fact]
        public async System.Threading.Tasks.Task FullStackedAreaSeries_DataLabels_Render()
        {
            using var ctx = CreateChartContext();
            var chart = RenderSeriesChart<RadzenFullStackedAreaSeries<DataItem>>(ctx);
            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));

            Assert.Equal(3, LabelGroups(chart.Markup));
            Assert.Equal(3, Chips(chart.Markup));
        }

        [Fact]
        public async System.Threading.Tasks.Task FullStackedLineSeries_DataLabels_Render()
        {
            using var ctx = CreateChartContext();
            var chart = RenderSeriesChart<RadzenFullStackedLineSeries<DataItem>>(ctx);
            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));

            Assert.Equal(3, LabelGroups(chart.Markup));
        }

        static DataItem[] MixedSignData => new[]
        {
            new DataItem { Category = "A", Value = 10 },
            new DataItem { Category = "B", Value = -10 },
            new DataItem { Category = "C", Value = 15 },
        };

        [Fact]
        public async System.Threading.Tasks.Task ColumnSeries_DataLabels_RenderChips_AndFormat()
        {
            using var ctx = CreateChartContext();
            var chart = RenderSeriesChart<RadzenColumnSeries<DataItem>>(ctx, d => d.Add(x => x.FormatString, "{0:0}u"));
            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));

            Assert.Equal(3, LabelGroups(chart.Markup));
            Assert.Equal(3, Chips(chart.Markup));
            Assert.Contains("20u", chart.Markup);
        }

        [Fact]
        public async System.Threading.Tasks.Task ColumnSeries_AutoPosition_IsSignAware()
        {
            using var ctx = CreateChartContext();
            var chart = RenderSeriesChart<RadzenColumnSeries<DataItem>>(ctx, data: MixedSignData);
            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));

            var series = chart.FindComponent<RadzenColumnSeries<DataItem>>().Instance;
            var labels = series.GetDataLabels(0, 0).ToArray();

            // SVG y grows downward: positive columns label above the value end, negative below.
            Assert.True(labels[0].Position.Y < labels[0].Anchor!.Y, "positive label should render above the column end");
            Assert.True(labels[1].Position.Y > labels[1].Anchor!.Y, "negative label should render below the column end");
        }

        [Fact]
        public async System.Threading.Tasks.Task ColumnSeries_ExplicitPositions_PlaceRelativeToColumn()
        {
            using var ctx = CreateChartContext();
            var chart = RenderSeriesChart<RadzenColumnSeries<DataItem>>(ctx);
            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));

            var series = chart.FindComponent<RadzenColumnSeries<DataItem>>().Instance;
            var top = series.GetDataLabels(0, 0, DataLabelPosition.Top).ToArray();
            var inside = series.GetDataLabels(0, 0, DataLabelPosition.Inside).ToArray();
            var center = series.GetDataLabels(0, 0, DataLabelPosition.Center).ToArray();

            // Positive columns: Top is above the value end, Inside just below it, Center between end and base.
            // Compare on item B (the max) - item A sits at the axis minimum, so its column has zero height.
            Assert.True(top[1].Position.Y < top[1].Anchor!.Y);
            Assert.True(inside[1].Position.Y > inside[1].Anchor!.Y);
            Assert.True(center[1].Position.Y > inside[1].Position.Y);
        }

        static IRenderedComponent<RadzenChart> RenderTwoSeriesChart<TSeries>(TestContext ctx)
            where TSeries : Radzen.Blazor.CartesianSeries<DataItem>
        {
            return ctx.RenderComponent<RadzenChart>(p =>
            {
                p.AddChildContent<TSeries>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Data, SampleData)
                    .AddChildContent<RadzenSeriesDataLabels>(d => d.Add(x => x.AllowOverlap, true)));
                p.AddChildContent<TSeries>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Data, SampleData)
                    .AddChildContent<RadzenSeriesDataLabels>(d => d.Add(x => x.AllowOverlap, true)));
            });
        }

        [Fact]
        public async System.Threading.Tasks.Task StackedColumnSeries_AutoPosition_IsSegmentCenter()
        {
            using var ctx = CreateChartContext();
            var chart = RenderTwoSeriesChart<RadzenStackedColumnSeries<DataItem>>(ctx);
            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));

            Assert.Equal(6, LabelGroups(chart.Markup));

            var series = chart.FindComponent<RadzenStackedColumnSeries<DataItem>>().Instance;
            var auto = series.GetDataLabels(0, 0).ToArray();
            var center = series.GetDataLabels(0, 0, DataLabelPosition.Center).ToArray();

            Assert.Equal(center[0].Position.Y, auto[0].Position.Y);
        }

        [Fact]
        public async System.Threading.Tasks.Task StackedColumnSeries_TopBottom_StayInsideSegment()
        {
            using var ctx = CreateChartContext();
            var chart = RenderTwoSeriesChart<RadzenStackedColumnSeries<DataItem>>(ctx);
            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));

            // The second series stacks on top of the first - its segment is bounded by the first
            // series below and the plot edge above.
            var series = chart.FindComponents<RadzenStackedColumnSeries<DataItem>>()[1].Instance;
            var top = series.GetDataLabels(0, 0, DataLabelPosition.Top).ToArray();
            var bottom = series.GetDataLabels(0, 0, DataLabelPosition.Bottom).ToArray();
            var center = series.GetDataLabels(0, 0, DataLabelPosition.Center).ToArray();
            var inside = series.GetDataLabels(0, 0, DataLabelPosition.Inside).ToArray();

            for (var i = 0; i < center.Length; i++)
            {
                // Top hugs the upper edge from the inside - for positive segments the upper edge is the
                // value end, so Top must coincide with Inside (it previously rendered outside the segment).
                Assert.Equal(inside[i].Position.Y, top[i].Position.Y);
                // Top and Bottom inset symmetrically around the center, inside the segment.
                Assert.True(top[i].Position.Y < center[i].Position.Y, $"item {i}: top should be above center");
                Assert.True(bottom[i].Position.Y > center[i].Position.Y, $"item {i}: bottom should be below center");
                Assert.Equal(center[i].Position.Y - top[i].Position.Y, bottom[i].Position.Y - center[i].Position.Y, 6);
            }
        }

        [Fact]
        public async System.Threading.Tasks.Task FullStackedColumnSeries_DataLabels_ShowPercentages()
        {
            using var ctx = CreateChartContext();
            var chart = RenderTwoSeriesChart<RadzenFullStackedColumnSeries<DataItem>>(ctx);
            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));

            var texts = LabelTexts(chart.Markup);
            Assert.Equal(6, texts.Length);
            // Two identical series - every segment is 50% of its category.
            Assert.All(texts, t => Assert.Equal("50%", t));
        }

        static string[] LabelTextAnchors(string markup) =>
            Regex.Matches(markup, "<text[^>]*rz-series-data-label[^>]*>")
                .Select(m => Regex.Match(m.Value, "text-anchor=\"([a-z]+)\"").Groups[1].Value).ToArray();

        [Fact]
        public async System.Threading.Tasks.Task BarSeries_DataLabels_RenderChips_AndFormat()
        {
            using var ctx = CreateChartContext();
            var chart = RenderSeriesChart<RadzenBarSeries<DataItem>>(ctx, d => d.Add(x => x.FormatString, "{0:0}u"));
            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));

            Assert.Equal(3, LabelGroups(chart.Markup));
            Assert.Equal(3, Chips(chart.Markup));
            Assert.Contains("20u", chart.Markup);
        }

        [Fact]
        public async System.Threading.Tasks.Task BarSeries_AutoPosition_IsSignAware()
        {
            using var ctx = CreateChartContext();
            var chart = RenderSeriesChart<RadzenBarSeries<DataItem>>(ctx, data: MixedSignData);
            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));

            var series = chart.FindComponent<RadzenBarSeries<DataItem>>().Instance;
            var labels = series.GetDataLabels(0, 0).ToArray();

            // Positive bars grow right: label right of the value end, anchored start; negative mirrored.
            Assert.True(labels[0].Position.X > labels[0].Anchor!.X);
            Assert.Equal("start", labels[0].TextAnchor);
            Assert.True(labels[1].Position.X < labels[1].Anchor!.X);
            Assert.Equal("end", labels[1].TextAnchor);
        }

        [Fact]
        public async System.Threading.Tasks.Task BarSeries_ExplicitPositions_PlaceRelativeToBar()
        {
            using var ctx = CreateChartContext();
            var chart = RenderSeriesChart<RadzenBarSeries<DataItem>>(ctx);
            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));

            var series = chart.FindComponent<RadzenBarSeries<DataItem>>().Instance;
            var inside = series.GetDataLabels(0, 0, DataLabelPosition.Inside).ToArray();
            var center = series.GetDataLabels(0, 0, DataLabelPosition.Center).ToArray();
            var top = series.GetDataLabels(0, 0, DataLabelPosition.Top).ToArray();

            // Positive bars: Inside is just left of the value end (right-aligned), Center further left, Top above.
            Assert.True(inside[1].Position.X < inside[1].Anchor!.X);
            Assert.Equal("end", inside[1].TextAnchor);
            Assert.True(center[1].Position.X < inside[1].Position.X);
            Assert.True(top[1].Position.Y < top[1].Anchor!.Y);
        }

        [Fact]
        public async System.Threading.Tasks.Task BarSeries_AutoPosition_FlipsInside_AtPlotEdge()
        {
            using var ctx = CreateChartContext();
            // B=20 is the scale maximum - its label would clip the plot's right edge and must flip
            // inside the bar with a swapped text anchor.
            var chart = RenderSeriesChart<RadzenBarSeries<DataItem>>(ctx);
            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));

            var anchors = LabelTextAnchors(chart.Markup);
            Assert.Equal(3, anchors.Length);
            Assert.Equal(new[] { "start", "end", "start" }, anchors);
        }

        [Fact]
        public async System.Threading.Tasks.Task StackedBarSeries_AutoPosition_IsSegmentCenter()
        {
            using var ctx = CreateChartContext();
            var chart = RenderTwoSeriesChart<RadzenStackedBarSeries<DataItem>>(ctx);
            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));

            Assert.Equal(6, LabelGroups(chart.Markup));

            var series = chart.FindComponent<RadzenStackedBarSeries<DataItem>>().Instance;
            var auto = series.GetDataLabels(0, 0).ToArray();
            var center = series.GetDataLabels(0, 0, DataLabelPosition.Center).ToArray();

            Assert.Equal(center[0].Position.X, auto[0].Position.X);
        }

        [Fact]
        public async System.Threading.Tasks.Task FullStackedBarSeries_DataLabels_Render()
        {
            using var ctx = CreateChartContext();
            var chart = RenderTwoSeriesChart<RadzenFullStackedBarSeries<DataItem>>(ctx);
            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));

            Assert.Equal(6, LabelGroups(chart.Markup));
        }

        [Fact]
        public async System.Threading.Tasks.Task ScatterSeries_DataLabels_RenderChips_AndFormat()
        {
            using var ctx = CreateChartContext();
            var chart = RenderSeriesChart<RadzenScatterSeries<DataItem>>(ctx, d => d.Add(x => x.FormatString, "{0:0}u"));
            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));

            Assert.Equal(3, LabelGroups(chart.Markup));
            Assert.Equal(3, Chips(chart.Markup));
            Assert.Contains("20u", chart.Markup);
        }

        static IRenderedComponent<RadzenChart> RenderBubbleChart(TestContext ctx)
        {
            return ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenBubbleSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Value2))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.SizeProperty, nameof(DataItem.Max))
                    .Add(x => x.Data, SampleData)
                    .AddChildContent<RadzenSeriesDataLabels>(d => d.Add(x => x.AllowOverlap, true))));
        }

        [Fact]
        public async System.Threading.Tasks.Task BubbleSeries_AutoPosition_ClearsBubbleEdge()
        {
            using var ctx = CreateChartContext();
            var chart = RenderBubbleChart(ctx);
            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));

            var series = chart.FindComponent<RadzenBubbleSeries<DataItem>>().Instance;
            var labels = series.GetDataLabels(0, 0).ToArray();

            // Label distance from the bubble center grows with its radius, so every label clears the
            // bubble edge instead of using the fixed scatter offset. Items sort by numeric X (Value2:
            // A=20, C=25, B=30) and sizes grow in the same order (Max: A=25, C=30, B=35).
            var offsets = labels.Select(l => l.Anchor!.Y - l.Position.Y).ToArray();
            Assert.All(offsets, o => Assert.True(o > 12, "label should be offset beyond the bubble edge"));
            Assert.True(offsets[0] < offsets[1], "the larger bubble should push its label further out");
            Assert.True(offsets[1] < offsets[2]);
        }

        [Fact]
        public async System.Threading.Tasks.Task BubbleSeries_CenterPosition_IsBubbleCenter()
        {
            using var ctx = CreateChartContext();
            var chart = RenderBubbleChart(ctx);
            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));

            var series = chart.FindComponent<RadzenBubbleSeries<DataItem>>().Instance;
            var labels = series.GetDataLabels(0, 0, DataLabelPosition.Center).ToArray();

            Assert.All(labels, l => Assert.Equal(l.Anchor!.Y, l.Position.Y));
        }

        static IRenderedComponent<RadzenChart> RenderPieChart<TSeries>(TestContext ctx,
            System.Action<ComponentParameterCollectionBuilder<RadzenSeriesDataLabels>>? labels = null)
            where TSeries : RadzenPieSeries<DataItem>
        {
            return ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<TSeries>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Data, SampleData)
                    .AddChildContent<RadzenSeriesDataLabels>(d => labels?.Invoke(d))));
        }

        [Fact]
        public async System.Threading.Tasks.Task PieSeries_DataLabels_RenderChips_AndFormat()
        {
            using var ctx = CreateChartContext();
            var chart = RenderPieChart<RadzenPieSeries<DataItem>>(ctx, d => d.Add(x => x.FormatString, "{0:0}u"));
            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));

            Assert.Equal(3, LabelGroups(chart.Markup));
            Assert.Equal(3, Chips(chart.Markup));
            Assert.Contains("20u", chart.Markup);
        }

        static double Distance(Point a, Point b)
        {
            var dx = a.X - b.X;
            var dy = a.Y - b.Y;
            return System.Math.Sqrt(dx * dx + dy * dy);
        }

        [Fact]
        public async System.Threading.Tasks.Task PieSeries_Positions_MoveAlongTheRadius()
        {
            using var ctx = CreateChartContext();
            var chart = RenderPieChart<RadzenPieSeries<DataItem>>(ctx);
            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));

            var series = chart.FindComponent<RadzenPieSeries<DataItem>>().Instance;
            var auto = series.GetDataLabels(0, 0).ToArray();
            var inside = series.GetDataLabels(0, 0, DataLabelPosition.Inside).ToArray();
            var center = series.GetDataLabels(0, 0, DataLabelPosition.Center).ToArray();

            for (var i = 0; i < auto.Length; i++)
            {
                // The anchor is the point on the slice's outer edge; positions move along the mid-angle ray.
                Assert.Equal(16, Distance(auto[i].Position, auto[i].Anchor!), 6);
                Assert.Equal(16, Distance(inside[i].Position, inside[i].Anchor!), 6);
                // Auto sits outside the edge and Inside inside it - on opposite sides of the anchor.
                Assert.Equal(32, Distance(auto[i].Position, inside[i].Position), 6);
                // Center is the radial middle of the slice - deeper inside than the Inside inset.
                Assert.True(Distance(center[i].Position, center[i].Anchor!) > 16, $"slice {i}: Center should be deeper than Inside");
            }

            // Inside and Center labels read better centered on the slice's mid-angle.
            Assert.All(inside, l => Assert.Equal("middle", l.TextAnchor));
            Assert.All(center, l => Assert.Equal("middle", l.TextAnchor));
        }

        [Fact]
        public async System.Threading.Tasks.Task FunnelSeries_DataLabels_RenderChips_AndFormat()
        {
            using var ctx = CreateChartContext();
            var chart = RenderSeriesChart<RadzenFunnelSeries<DataItem>>(ctx, d => d.Add(x => x.FormatString, "{0:0}u"));
            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));

            Assert.Equal(3, LabelGroups(chart.Markup));
            Assert.Equal(3, Chips(chart.Markup));
            Assert.Contains("20u", chart.Markup);
        }

        [Fact]
        public async System.Threading.Tasks.Task FunnelSeries_TopBottom_StayInsideSegment()
        {
            using var ctx = CreateChartContext();
            var chart = RenderSeriesChart<RadzenFunnelSeries<DataItem>>(ctx);
            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));

            var series = chart.FindComponent<RadzenFunnelSeries<DataItem>>().Instance;
            var top = series.GetDataLabels(0, 0, DataLabelPosition.Top).ToArray();
            var bottom = series.GetDataLabels(0, 0, DataLabelPosition.Bottom).ToArray();
            var center = series.GetDataLabels(0, 0, DataLabelPosition.Center).ToArray();

            for (var i = 0; i < center.Length; i++)
            {
                // Top and Bottom inset symmetrically inside the segment, never outside it.
                Assert.True(top[i].Position.Y < center[i].Position.Y, $"segment {i}: top should be above center");
                Assert.True(bottom[i].Position.Y > center[i].Position.Y, $"segment {i}: bottom should be below center");
                Assert.Equal(center[i].Position.Y - top[i].Position.Y, bottom[i].Position.Y - center[i].Position.Y, 6);
            }
        }

        [Fact]
        public async System.Threading.Tasks.Task PyramidSeries_DataLabels_RenderChips_AndFormat()
        {
            using var ctx = CreateChartContext();
            var chart = RenderSeriesChart<RadzenPyramidSeries<DataItem>>(ctx, d => d.Add(x => x.FormatString, "{0:0}u"));
            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));

            Assert.True(LabelGroups(chart.Markup) > 0);
            Assert.Contains("20u", chart.Markup);
        }

        [Fact]
        public async System.Threading.Tasks.Task DonutSeries_CenterPosition_IsRingMidpoint()
        {
            using var ctx = CreateChartContext();
            var pieChart = RenderPieChart<RadzenPieSeries<DataItem>>(ctx);
            await pieChart.InvokeAsync(() => pieChart.Instance.Resize(400, 300));
            var donutChart = RenderPieChart<RadzenDonutSeries<DataItem>>(ctx);
            await donutChart.InvokeAsync(() => donutChart.Instance.Resize(400, 300));

            var pie = pieChart.FindComponent<RadzenPieSeries<DataItem>>().Instance;
            var donut = donutChart.FindComponent<RadzenDonutSeries<DataItem>>().Instance;
            var pieCenter = pie.GetDataLabels(0, 0, DataLabelPosition.Center).ToArray();
            var donutCenter = donut.GetDataLabels(0, 0, DataLabelPosition.Center).ToArray();

            for (var i = 0; i < pieCenter.Length; i++)
            {
                // Pie Center is at half the outer radius (anchor-to-label = outer/2); the default donut
                // hole is half the radius, so its ring midpoint is at 75% (anchor-to-label = outer/4).
                var pieDepth = Distance(pieCenter[i].Position, pieCenter[i].Anchor!);
                var donutDepth = Distance(donutCenter[i].Position, donutCenter[i].Anchor!);

                Assert.Equal(pieDepth, donutDepth * 2, 6);
            }
        }
    }
}
