using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
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
        ctx.JSInterop.Setup<Radzen.Blazor.Rendering.Rect>("Radzen.createChart", _ => true).SetResult(new Radzen.Blazor.Rendering.Rect { Left = 0, Top = 0, Width = 200, Height = 200 });
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

    private class MultiAxisItem
    {
        public string Month { get; set; } = "";
        public double Revenue { get; set; }
        public double Rate { get; set; }
    }

    // Tall columns (near the axis max) so the line markers, plotted on a separate 0-100 axis, sit
    // visually inside the columns - the exact overlap that used to make the column win the hover.
    private static readonly MultiAxisItem[] TallBarData =
    {
        new() { Month = "Jan", Revenue = 9000, Rate = 40 },
        new() { Month = "Feb", Revenue = 8000, Rate = 45 },
        new() { Month = "Mar", Revenue = 9500, Rate = 50 },
    };

    private static TestContext CreateChartContext()
    {
        var ctx = new TestContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.JSInterop.Setup<Radzen.Blazor.Rendering.Rect>("Radzen.createChart", _ => true)
            .SetResult(new Radzen.Blazor.Rendering.Rect { Left = 0, Top = 0, Width = 600, Height = 400 });
        ctx.Services.AddScoped<TooltipService>();
        return ctx;
    }

    [Fact]
    public void Line_Marker_On_Top_Of_Column_Wins_Hover_Selection()
    {
        using var ctx = CreateChartContext();

        var chart = ctx.RenderComponent<RadzenChart>(parameters => parameters
            .AddChildContent<RadzenColumnSeries<MultiAxisItem>>(series => series
                .Add(p => p.CategoryProperty, nameof(MultiAxisItem.Month))
                .Add(p => p.ValueProperty, nameof(MultiAxisItem.Revenue))
                .Add(p => p.Data, TallBarData))
            .AddChildContent<RadzenLineSeries<MultiAxisItem>>(series => series
                .Add(p => p.CategoryProperty, nameof(MultiAxisItem.Month))
                .Add(p => p.ValueProperty, nameof(MultiAxisItem.Rate))
                .Add(p => p.ValueAxisName, "rate")
                .Add(p => p.Data, TallBarData))
            .AddChildContent<RadzenValueAxis>(a => a
                .Add(p => p.Min, 0d).Add(p => p.Max, 10000d).Add(p => p.Step, 2000d))
            .AddChildContent<RadzenValueAxis>(a => a
                .Add(p => p.Name, "rate").Add(p => p.Min, 0d).Add(p => p.Max, 100d).Add(p => p.Step, 20d)));

        var instance = chart.Instance;
        var line = instance.Series.Single(s => (s as IChartValueAxisSeries)?.ValueAxisName == "rate");
        var column = instance.Series.Single(s => s is IChartColumnSeries);

        // A line data point that sits well inside the (taller) column for that category.
        var item = TallBarData[2]; // Mar: column at ~95% height, line at 50%
        var linePoint = line.GetTooltipPosition(item);

        // Hovering the marker selects the line, even though the column also contains the point.
        var (hovered, _) = instance.FindClosestSeries(linePoint.X, linePoint.Y, 25);
        Assert.Same(line, hovered);

        // Hovering the bar away from the line (well below the marker) still selects the column.
        var (barHovered, _) = instance.FindClosestSeries(linePoint.X, linePoint.Y + 80, 25);
        Assert.Same(column, barHovered);
    }

    [Fact]
    public void Chart_Renders_AllowZoom_DataAttribute_And_Updates_On_Runtime_Change()
    {
        using var ctx = CreateChartContext();

        var chart = ctx.RenderComponent<RadzenChart>(parameters => parameters
            .AddChildContent<RadzenColumnSeries<MultiAxisItem>>(series => series
                .Add(p => p.CategoryProperty, nameof(MultiAxisItem.Month))
                .Add(p => p.ValueProperty, nameof(MultiAxisItem.Revenue))
                .Add(p => p.Data, TallBarData)));

        var root = chart.Find("[data-allow-zoom]");
        Assert.Equal("false", root.GetAttribute("data-allow-zoom"));
        Assert.Equal("0", root.GetAttribute("data-zoom-start"));
        Assert.Equal("1", root.GetAttribute("data-zoom-end"));

        chart.SetParametersAndRender(parameters => parameters.Add(p => p.AllowZoom, true));
        Assert.Equal("true", chart.Find("[data-allow-zoom]").GetAttribute("data-allow-zoom"));

        chart.SetParametersAndRender(parameters => parameters.Add(p => p.AllowZoom, false));
        Assert.Equal("false", chart.Find("[data-allow-zoom]").GetAttribute("data-allow-zoom"));
    }
}
