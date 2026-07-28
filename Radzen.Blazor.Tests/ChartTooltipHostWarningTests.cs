using System.Linq;
using System.Threading.Tasks;
using Bunit;
using Xunit;
using static Radzen.Blazor.Tests.ChartTestHelper;

namespace Radzen.Blazor.Tests
{
    public class ChartTooltipHostWarningTests
    {
        static IRenderedComponent<RadzenChart> RenderChart(TestContext ctx)
        {
            return ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenLineSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Data, SampleData)));
        }

        static (double X, double Y) MiddlePoint(IRenderedComponent<RadzenChart> chart)
        {
            var translate = System.Text.RegularExpressions.Regex.Match(chart.Markup, @"translate\(([\d.]+),\s*([\d.]+)\)");
            var ml = double.Parse(translate.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
            var mt = double.Parse(translate.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture);

            var path = System.Text.RegularExpressions.Regex.Match(chart.Markup, @"rz-line-series[\s\S]*?M\s+[\d.]+\s+[\d.]+\s+L\s+([\d.]+)\s+([\d.]+)");
            var x = double.Parse(path.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
            var y = double.Parse(path.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture);

            return (ml + x, mt + y);
        }

        static int Warnings(TestContext ctx)
        {
            return ctx.JSInterop.Invocations.Count(i => i.Identifier == "console.warn");
        }

        [Fact]
        public async Task MissingChartTooltipHost_WarnsOnceInConsole()
        {
            using var ctx = CreateChartContext();
            var chart = RenderChart(ctx);

            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));
            var pos = MiddlePoint(chart);
            await chart.InvokeAsync(() => chart.Instance.MouseMove(pos.X, pos.Y));

            Assert.Equal(1, Warnings(ctx));

            await chart.InvokeAsync(() => chart.Instance.MouseMove(pos.X + 1, pos.Y));

            Assert.Equal(1, Warnings(ctx));
        }

        [Fact]
        public async Task PresentChartTooltipHost_DoesNotWarn()
        {
            using var ctx = CreateChartContext();
            ctx.JSInterop.SetupVoid("Radzen.openChartTooltip", _ => true);
            ctx.RenderComponent<RadzenChartTooltip>();
            var chart = RenderChart(ctx);

            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));
            var pos = MiddlePoint(chart);
            await chart.InvokeAsync(() => chart.Instance.MouseMove(pos.X, pos.Y));

            Assert.True(ctx.JSInterop.Invocations.Any(i => i.Identifier == "Radzen.openChartTooltip"), "positive control: tooltip should open near a data point");
            Assert.Equal(0, Warnings(ctx));
        }
    }
}
