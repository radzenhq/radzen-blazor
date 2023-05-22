using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
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
        foreach (var _ in Enumerable.Range(0, 10))
        {
            await chart.InvokeAsync(() => chart.Instance.MouseMove(100, 80));
            Assert.Contains("<div class=\"rz-chart-tooltip", chart.Markup);
            await chart.InvokeAsync(() => chart.Instance.MouseMove(0, 0));
            Assert.DoesNotContain("<div class=\"rz-chart-tooltip", chart.Markup);
        }
        output.WriteLine($"Time took: {stopwatch.Elapsed}");
    }
}