using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Radzen.Blazor.Rendering;
using Xunit;
using Xunit.Abstractions;

namespace Radzen.Blazor.Tests;

public class ChartTests
{
    private readonly ITestOutputHelper output;
    public ChartTests(ITestOutputHelper output)
    {
        this.output = output;
    }

    [Fact(Timeout = 30000)]
    public async Task Chart_Tooltip_Performance()
    {
        using var ctx = new TestContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.JSInterop.Setup<Rect>("Radzen.createChart", _ => true).SetResult(new Rect {Left = 0, Top = 0, Width = 200, Height = 200});
        ctx.Services.AddScoped<TooltipService>();
        ctx.JSInterop.SetupVoid("Radzen.openChartTooltip", _ => true);
        ctx.RenderComponent<RadzenChartTooltip>();

        var seriesData = Enumerable.Range(0, 5000).Select(i => new Point { X = i, Y = i });
        var chart = ctx.RenderComponent<RadzenChart>(chartParameters =>
            chartParameters
                .AddChildContent<RadzenLineSeries<Point>>(seriesParameters =>
                    seriesParameters
                        .Add(p => p.CategoryProperty, nameof(Point.X))
                        .Add(p => p.ValueProperty, nameof(Point.Y))
                        .Add(p => p.Data, seriesData))
                .AddChildContent<RadzenCategoryAxis>(axisParameters =>
                    axisParameters
                        .Add(p => p.Step, 100)
                        .Add(p => p.Formatter, x =>
                        {
                            Thread.Sleep(100);
                            return $"{x}";
                        })));

        var stopwatch = Stopwatch.StartNew();
        foreach (var invocation in Enumerable.Range(0, 10))
        {
            await chart.InvokeAsync(() => chart.Instance.MouseMove(100, 80));
            Assert.Equal((invocation + 1) * 2, ctx.JSInterop.Invocations.Count(x => x.Identifier == "Radzen.openChartTooltip"));
            await chart.InvokeAsync(() => chart.Instance.MouseMove(0, 0));
            Assert.Equal(invocation + 1, ctx.JSInterop.Invocations.Count(x => x.Identifier == "Radzen.closeTooltip"));
        }
        output.WriteLine($"Time took: {stopwatch.Elapsed}");
    }
}