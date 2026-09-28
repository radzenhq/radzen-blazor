using System.Globalization;
using System.Linq;
using Bunit;
using Xunit;
using static Radzen.Blazor.Tests.ChartTestHelper;

namespace Radzen.Blazor.Tests
{
    public class ChartZoomHitTestTests
    {
        static readonly DataItem[] Months = new[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" }
            .Select((month, index) => new DataItem { Category = month, Value = 10 + index })
            .ToArray();

        static IRenderedComponent<RadzenChart> RenderZoomedChart(TestContext ctx, string group)
        {
            return ctx.RenderComponent<RadzenChart>(p => p
                .Add(x => x.SyncGroup, group)
                .Add(x => x.AllowZoom, true)
                .Add(x => x.ViewStart, 0.55)
                .Add(x => x.ViewEnd, 1)
                .AddChildContent<RadzenLineSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Data, Months)));
        }

        static (double Left, double Bottom) PlotEdges(IRenderedComponent<RadzenChart> chart)
        {
            var transform = chart.FindAll("svg g[transform^=translate]").First().GetAttribute("transform")!;
            var offsets = transform.Substring("translate(".Length).TrimEnd(')').Split(',');
            var marginLeft = double.Parse(offsets[0], CultureInfo.InvariantCulture);
            var marginTop = double.Parse(offsets[1], CultureInfo.InvariantCulture);
            var axis = chart.Find(".rz-category-axis .rz-line").GetAttribute("d")!.Split(' ');
            return (marginLeft + double.Parse(axis[1], CultureInfo.InvariantCulture), marginTop + double.Parse(axis[2], CultureInfo.InvariantCulture));
        }

        [Fact]
        public async System.Threading.Tasks.Task ZoomedChart_TooltipIgnoresPointsScrolledOutOfView()
        {
            using var ctx = CreateChartContext();
            ctx.JSInterop.SetupVoid("Radzen.openChartTooltip", _ => true);
            ctx.RenderComponent<RadzenChartTooltip>();

            var chart = RenderZoomedChart(ctx, $"test-{System.Guid.NewGuid():N}");
            await chart.InvokeAsync(() => chart.Instance.Resize(600, 300));

            Assert.DoesNotContain("Jul", chart.Markup);

            var (left, bottom) = PlotEdges(chart);
            await chart.InvokeAsync(() => chart.Instance.MouseMove(left + 1, bottom - 40));

            Assert.Contains("rz-chart-category-tooltip", chart.Markup);
            Assert.Contains("Aug", chart.Markup);
            Assert.DoesNotContain("Jul", chart.Markup);
        }

        [Fact]
        public async System.Threading.Tasks.Task ZoomedChart_SyncedHoverIgnoresPointsScrolledOutOfView()
        {
            using var ctx = CreateChartContext();
            ctx.JSInterop.SetupVoid("Radzen.openChartTooltip", _ => true);
            ctx.RenderComponent<RadzenChartTooltip>();

            var group = $"test-{System.Guid.NewGuid():N}";
            var source = RenderZoomedChart(ctx, group);
            var receiver = RenderZoomedChart(ctx, group);
            await source.InvokeAsync(() => source.Instance.Resize(600, 300));
            await receiver.InvokeAsync(() => receiver.Instance.Resize(600, 300));

            var (left, bottom) = PlotEdges(source);
            await source.InvokeAsync(() => source.Instance.MouseMove(left + 1, bottom - 40));

            Assert.Contains("rz-chart-category-tooltip", receiver.Markup);
            Assert.Contains("rz-active-point", receiver.Markup);
            Assert.Contains("Aug", receiver.Markup);
            Assert.DoesNotContain("Jul", receiver.Markup);
        }

        [Fact]
        public async System.Threading.Tasks.Task UnzoomedChart_TooltipStillSnapsToNearestPoint()
        {
            using var ctx = CreateChartContext();
            ctx.JSInterop.SetupVoid("Radzen.openChartTooltip", _ => true);
            ctx.RenderComponent<RadzenChartTooltip>();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .Add(x => x.SyncGroup, $"test-{System.Guid.NewGuid():N}")
                .AddChildContent<RadzenLineSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Data, Months)));
            await chart.InvokeAsync(() => chart.Instance.Resize(600, 300));

            var (left, bottom) = PlotEdges(chart);
            await chart.InvokeAsync(() => chart.Instance.MouseMove(left + 1, bottom - 40));

            Assert.Contains("rz-chart-category-tooltip", chart.Markup);
            Assert.Contains(">Jan<", chart.Markup.Replace("\n", "").Replace(" ", ""));
        }
    }
}
