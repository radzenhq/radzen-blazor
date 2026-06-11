using System.Linq;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using static Radzen.Blazor.Tests.ChartTestHelper;

namespace Radzen.Blazor.Tests
{
    public class ChartTooltipTriggerTests
    {
        static IRenderedComponent<RadzenChart> RenderChart(TestContext ctx, ChartTooltipTrigger trigger, bool crosshair)
        {
            // CreateChartContext already registers TooltipService.
            ctx.JSInterop.SetupVoid("Radzen.openChartTooltip", _ => true);
            ctx.RenderComponent<RadzenChartTooltip>();

            return ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenChartTooltipOptions>(t => t
                    .Add(x => x.Trigger, trigger))
                .AddChildContent<RadzenLineSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Data, SampleData))
                .AddChildContent<RadzenCategoryAxis>(a => a
                    .AddChildContent<RadzenAxisCrosshair>(c => c
                        .Add(x => x.Visible, crosshair))));
        }

        static int TooltipOpens(TestContext ctx)
        {
            return ctx.JSInterop.Invocations.Count(i => i.Identifier == "Radzen.openChartTooltip");
        }

        // Reads the chart-relative coordinates of the LAST data point and a position vertically far
        // from every point at the same X, from the rendered line path and plot translate.
        static (double NearX, double NearY, double FarX, double FarY) Positions(IRenderedComponent<RadzenChart> chart)
        {
            var translate = System.Text.RegularExpressions.Regex.Match(chart.Markup, @"translate\(([\d.]+),\s*([\d.]+)\)");
            var ml = double.Parse(translate.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
            var mt = double.Parse(translate.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture);

            var path = System.Text.RegularExpressions.Regex.Match(chart.Markup, @"rz-line-series[\s\S]*?M\s+([\d.]+)\s+([\d.]+)\s+L\s+([\d.]+)\s+([\d.]+)\s+L\s+([\d.]+)\s+([\d.]+)");
            var xs = new[] { 1, 3, 5 }.Select(g => double.Parse(path.Groups[g].Value, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
            var ys = new[] { 2, 4, 6 }.Select(g => double.Parse(path.Groups[g].Value, System.Globalization.CultureInfo.InvariantCulture)).ToArray();

            // Near: directly on the middle data point. Far: same X, 60px below it (outside the 25px tolerance,
            // still inside the plot because values span the full plot height).
            var farY = (ys[1] + ys[0]) / 2 + 60;
            return (ml + xs[1], mt + ys[1], ml + xs[1] + 8, mt + farY);
        }

        [Fact]
        public async System.Threading.Tasks.Task PointTrigger_NearSeries_ShowsTooltip()
        {
            using var ctx = CreateChartContext();
            var chart = RenderChart(ctx, ChartTooltipTrigger.Point, crosshair: false);

            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));
            var pos = Positions(chart);
            await chart.InvokeAsync(() => chart.Instance.MouseMove(pos.NearX, pos.NearY));

            Assert.True(TooltipOpens(ctx) > 0, "positive control: tooltip should open near a data point");
        }

        [Fact]
        public async System.Threading.Tasks.Task PointTrigger_FarFromSeries_DoesNotShowTooltip()
        {
            using var ctx = CreateChartContext();
            var chart = RenderChart(ctx, ChartTooltipTrigger.Point, crosshair: true);

            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));
            var pos = Positions(chart);
            await chart.InvokeAsync(() => chart.Instance.MouseMove(pos.FarX, pos.FarY));

            Assert.Equal(0, TooltipOpens(ctx));
        }

        [Fact]
        public async System.Threading.Tasks.Task AxisTrigger_FarFromSeries_ShowsTooltip()
        {
            using var ctx = CreateChartContext();
            var chart = RenderChart(ctx, ChartTooltipTrigger.Axis, crosshair: false);

            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));
            var pos = Positions(chart);
            await chart.InvokeAsync(() => chart.Instance.MouseMove(pos.FarX, pos.FarY));

            // Axis mode renders an in-chart category box instead of the popup tooltip.
            Assert.Equal(0, TooltipOpens(ctx));
            Assert.Contains("rz-chart-category-tooltip", chart.Markup);
        }

        [Fact]
        public async System.Threading.Tasks.Task AutoTrigger_WithCrosshair_ShowsTooltipAnywhereInPlot()
        {
            using var ctx = CreateChartContext();
            var chart = RenderChart(ctx, ChartTooltipTrigger.Auto, crosshair: true);

            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));
            var pos = Positions(chart);
            await chart.InvokeAsync(() => chart.Instance.MouseMove(pos.FarX, pos.FarY));

            // Auto resolves to axis mode when the crosshair is on - in-chart box, no popup.
            Assert.Equal(0, TooltipOpens(ctx));
            Assert.Contains("rz-chart-category-tooltip", chart.Markup);
        }

        [Fact]
        public async System.Threading.Tasks.Task AutoTrigger_WithoutCrosshair_BehavesLikePoint()
        {
            using var ctx = CreateChartContext();
            var chart = RenderChart(ctx, ChartTooltipTrigger.Auto, crosshair: false);

            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));
            var pos = Positions(chart);
            await chart.InvokeAsync(() => chart.Instance.MouseMove(pos.FarX, pos.FarY));

            Assert.Equal(0, TooltipOpens(ctx));
            Assert.DoesNotContain("rz-chart-category-tooltip", chart.Markup);
        }

        [Fact]
        public async System.Threading.Tasks.Task AxisTrigger_CursorInMargin_ShowsNothing()
        {
            using var ctx = CreateChartContext();
            var chart = RenderChart(ctx, ChartTooltipTrigger.Axis, crosshair: true);

            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));
            // Above the plot area (inside the chart element, outside the plot).
            await chart.InvokeAsync(() => chart.Instance.MouseMove(200, 2));

            Assert.Equal(0, TooltipOpens(ctx));
            Assert.DoesNotContain("rz-chart-category-tooltip", chart.Markup);
            Assert.DoesNotContain("rz-chart-crosshair", chart.Markup);
        }
    }
}
