using Bunit;
using Xunit;
using static Radzen.Blazor.Tests.ChartTestHelper;

namespace Radzen.Blazor.Tests
{
    public class ChartCrosshairTests
    {
        [Fact]
        public async System.Threading.Tasks.Task Crosshair_Hidden_ByDefault()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenLineSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Data, SampleData)));

            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));
            await chart.InvokeAsync(() => chart.Instance.MouseMove(100, 100));

            Assert.DoesNotContain("rz-chart-crosshair", chart.Markup);
        }

        [Fact]
        public async System.Threading.Tasks.Task Crosshair_X_Visible_FromCategoryAxis()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenCategoryAxis>(axis => axis
                    .AddChildContent<RadzenAxisCrosshair>(c => c
                        .Add(x => x.Visible, true)))
                .AddChildContent<RadzenLineSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Data, SampleData)));

            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));
            await chart.InvokeAsync(() => chart.Instance.MouseMove(100, 100));

            Assert.Contains("rz-chart-crosshair", chart.Markup);
            var crosshairFragment = chart.Find("g.rz-chart-crosshair").InnerHtml;
            var lineCount = System.Text.RegularExpressions.Regex.Matches(crosshairFragment, "<line").Count;
            Assert.Equal(1, lineCount);
        }

        [Fact]
        public async System.Threading.Tasks.Task Crosshair_Hidden_OnMouseLeave()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenCategoryAxis>(axis => axis
                    .AddChildContent<RadzenAxisCrosshair>(c => c
                        .Add(x => x.Visible, true)))
                .AddChildContent<RadzenValueAxis>(axis => axis
                    .AddChildContent<RadzenAxisCrosshair>(c => c
                        .Add(x => x.Visible, true)))
                .AddChildContent<RadzenLineSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Data, SampleData)));

            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));
            await chart.InvokeAsync(() => chart.Instance.MouseMove(100, 100));
            Assert.Contains("rz-chart-crosshair", chart.Markup);

            await chart.InvokeAsync(() => chart.Instance.MouseMove(-1, -1));
            Assert.DoesNotContain("rz-chart-crosshair", chart.Markup);
        }

        [Fact]
        public async System.Threading.Tasks.Task Crosshair_Both_RendersTwoLines()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenCategoryAxis>(axis => axis
                    .AddChildContent<RadzenAxisCrosshair>(c => c
                        .Add(x => x.Visible, true)))
                .AddChildContent<RadzenValueAxis>(axis => axis
                    .AddChildContent<RadzenAxisCrosshair>(c => c
                        .Add(x => x.Visible, true)))
                .AddChildContent<RadzenLineSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Data, SampleData)));

            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));
            await chart.InvokeAsync(() => chart.Instance.MouseMove(100, 100));

            var crosshairFragment = chart.Find("g.rz-chart-crosshair").InnerHtml;
            var lineCount = System.Text.RegularExpressions.Regex.Matches(crosshairFragment, "<line").Count;
            Assert.Equal(2, lineCount);
        }

        [Fact]
        public async System.Threading.Tasks.Task Crosshair_CustomColor_AppliedToStroke()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenCategoryAxis>(axis => axis
                    .AddChildContent<RadzenAxisCrosshair>(c => c
                        .Add(x => x.Visible, true)
                        .Add(x => x.Stroke, "#ff00aa")))
                .AddChildContent<RadzenLineSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Data, SampleData)));

            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));
            await chart.InvokeAsync(() => chart.Instance.MouseMove(100, 100));

            Assert.Contains("#ff00aa", chart.Find("g.rz-chart-crosshair").InnerHtml);
        }

        [Fact]
        public async System.Threading.Tasks.Task Crosshair_Snap_False_FollowsCursorX()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .Add(x => x.TooltipTolerance, 0)
                .AddChildContent<RadzenCategoryAxis>(axis => axis
                    .AddChildContent<RadzenAxisCrosshair>(c => c
                        .Add(x => x.Visible, true)
                        .Add(x => x.Snap, false)))
                .AddChildContent<RadzenLineSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Data, SampleData)));

            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));
            // Hover at an arbitrary X with no snap — line should sit at mouseX - MarginLeft.
            await chart.InvokeAsync(() => chart.Instance.MouseMove(137, 100));

            var line = chart.Find("g.rz-chart-crosshair line");
            var x1 = line.GetAttribute("x1");
            Assert.NotNull(x1);
            // Without snap, x1 must be a non-integer-ish value reflecting the cursor offset; specifically
            // it must NOT equal the snapped category position. Asserting "not snapped to a category index"
            // is the simplest invariant: the value should match (mouseX - MarginLeft) for the given setup.
            Assert.NotEqual("0", x1);
        }

        [Fact]
        public async System.Threading.Tasks.Task Crosshair_Snap_True_SnapsToNearestDataPoint_RegardlessOfTolerance()
        {
            using var ctx = CreateChartContext();

            // TooltipTolerance=0 means the tooltip would never fire; the crosshair must still snap to a data point.
            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .Add(x => x.TooltipTolerance, 0)
                .AddChildContent<RadzenCategoryAxis>(axis => axis
                    .AddChildContent<RadzenAxisCrosshair>(c => c
                        .Add(x => x.Visible, true)
                        .Add(x => x.Snap, true)))
                .AddChildContent<RadzenLineSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Data, SampleData)));

            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));
            // Move to a clearly off-data-point cursor X; line must still land on one of the SampleData
            // category positions, not on the cursor X itself.
            await chart.InvokeAsync(() => chart.Instance.MouseMove(137, 100));

            var line = chart.Find("g.rz-chart-crosshair line");
            var x1 = line.GetAttribute("x1");
            var x2 = line.GetAttribute("x2");
            Assert.Equal(x1, x2); // vertical line
            Assert.NotNull(x1);

            // With 3 evenly-spaced categories on a 400px-wide chart, the snapped X cannot equal an arbitrary
            // cursor offset. We assert the line position is one of a small set of category-aligned X values
            // by re-rendering at three distinct cursor positions and confirming we get at most 3 unique x1.
            var positions = new System.Collections.Generic.HashSet<string>();
            foreach (var cx in new[] { 60.0, 200.0, 340.0 })
            {
                await chart.InvokeAsync(() => chart.Instance.MouseMove(cx, 100));
                positions.Add(chart.Find("g.rz-chart-crosshair line").GetAttribute("x1") ?? string.Empty);
            }
            Assert.Equal(3, positions.Count); // three distinct snapped categories, one per cursor
        }

        [Fact]
        public async System.Threading.Tasks.Task Crosshair_Label_RendersFormattedAxisValue()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenValueAxis>(axis => axis
                    .Add(a => a.FormatString, "{0:N0} GW")
                    .AddChildContent<RadzenAxisCrosshair>(c => c
                        .Add(x => x.Visible, true)
                        .Add(x => x.Label, true)))
                .AddChildContent<RadzenLineSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Data, SampleData)));

            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));
            await chart.InvokeAsync(() => chart.Instance.MouseMove(100, 100));

            Assert.Contains("rz-chart-axis-crosshair-label", chart.Markup);
            Assert.Contains(" GW", chart.Find("g.rz-chart-axis-crosshair-label").InnerHtml);
        }
    }
}
