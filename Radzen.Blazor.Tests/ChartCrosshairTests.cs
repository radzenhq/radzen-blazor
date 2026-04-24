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
        public async System.Threading.Tasks.Task Crosshair_Visible_WhenCrosshairModeSetAndMouseInside()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenChartTooltipOptions>(t => t
                    .Add(x => x.CrosshairMode, CrosshairMode.X))
                .AddChildContent<RadzenLineSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Data, SampleData)));

            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));
            await chart.InvokeAsync(() => chart.Instance.MouseMove(100, 100));

            Assert.Contains("rz-chart-crosshair", chart.Markup);
            // Mode is X — exactly one vertical line
            var crosshairFragment = chart.Find("g.rz-chart-crosshair").InnerHtml;
            var lineCount = System.Text.RegularExpressions.Regex.Matches(crosshairFragment, "<line").Count;
            Assert.Equal(1, lineCount);
        }

        [Fact]
        public async System.Threading.Tasks.Task Crosshair_Hidden_OnMouseLeave()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenChartTooltipOptions>(t => t
                    .Add(x => x.CrosshairMode, CrosshairMode.Both))
                .AddChildContent<RadzenLineSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Data, SampleData)));

            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));
            await chart.InvokeAsync(() => chart.Instance.MouseMove(100, 100));
            Assert.Contains("rz-chart-crosshair", chart.Markup);

            // JS sends (-1,-1) on mouseleave
            await chart.InvokeAsync(() => chart.Instance.MouseMove(-1, -1));
            Assert.DoesNotContain("rz-chart-crosshair", chart.Markup);
        }

        [Fact]
        public async System.Threading.Tasks.Task Crosshair_Both_RendersTwoLines()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenChartTooltipOptions>(t => t
                    .Add(x => x.CrosshairMode, CrosshairMode.Both))
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
                .AddChildContent<RadzenChartTooltipOptions>(t => t
                    .Add(x => x.CrosshairMode, CrosshairMode.X)
                    .Add(x => x.CrosshairColor, "#ff00aa"))
                .AddChildContent<RadzenLineSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Data, SampleData)));

            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));
            await chart.InvokeAsync(() => chart.Instance.MouseMove(100, 100));

            Assert.Contains("#ff00aa", chart.Find("g.rz-chart-crosshair").InnerHtml);
        }

        [Fact]
        public async System.Threading.Tasks.Task SplitTooltip_Hidden_ByDefault()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenLineSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Data, SampleData)));

            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));
            await chart.InvokeAsync(() => chart.Instance.MouseMove(100, 100));

            Assert.DoesNotContain("rz-chart-split-tooltip", chart.Markup);
        }

        [Fact]
        public async System.Threading.Tasks.Task SplitTooltip_Visible_WhenSplitAndMouseInside()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .Add(p => p.TooltipTolerance, 200)
                .AddChildContent<RadzenChartTooltipOptions>(t => t
                    .Add(x => x.Split, true))
                .AddChildContent<RadzenLineSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Data, SampleData)));

            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));
            // Hover near a data point (point B is at chart coords ~(169.6, 32))
            await chart.InvokeAsync(() => chart.Instance.MouseMove(170, 35));

            Assert.Contains("rz-chart-split-tooltip", chart.Markup);
            Assert.Contains("rz-chart-split-tooltip-item", chart.Markup);
        }
    }
}
