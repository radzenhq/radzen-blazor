using System;
using System.Linq;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Radzen.Blazor.Tests;

public class BarSeriesMultipleAxesTests
{
    private class Item
    {
        public string Month { get; set; } = "";
        public double Revenue { get; set; }
        public double Count { get; set; }
    }

    private static readonly Item[] Data =
    {
        new() { Month = "Jan", Revenue = 234000, Count = 320 },
        new() { Month = "Feb", Revenue = 269000, Count = 358 },
        new() { Month = "Mar", Revenue = 233000, Count = 302 },
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

    private static IRenderedComponent<RadzenChart> RenderChart(TestContext ctx)
    {
        return ctx.RenderComponent<RadzenChart>(parameters => parameters
            .AddChildContent<RadzenBarSeries<Item>>(series => series
                .Add(p => p.Title, "Revenue")
                .Add(p => p.CategoryProperty, nameof(Item.Month))
                .Add(p => p.ValueProperty, nameof(Item.Revenue))
                .Add(p => p.Data, Data))
            .AddChildContent<RadzenBarSeries<Item>>(series => series
                .Add(p => p.Title, "Order Count")
                .Add(p => p.CategoryProperty, nameof(Item.Month))
                .Add(p => p.ValueProperty, nameof(Item.Count))
                .Add(p => p.ValueAxisName, "count")
                .Add(p => p.Data, Data))
            .AddChildContent<RadzenValueAxis>(a => a
                .Add(p => p.Name, "count")));
    }

    [Fact]
    public void Named_Axis_Scale_Is_Numeric_And_Horizontal()
    {
        using var ctx = CreateChartContext();
        var chart = RenderChart(ctx);
        var instance = chart.Instance;

        var additionalScale = instance.GetValueScale("count");

        Assert.IsNotType<OrdinalScale>(additionalScale);
        Assert.True(additionalScale.Input.End <= 500, $"Expected a numeric scale fitted to the count values but input was {additionalScale.Input.Start}..{additionalScale.Input.End}");
        Assert.Equal(instance.CategoryScale.Output.Start, additionalScale.Output.Start);
        Assert.Equal(instance.CategoryScale.Output.End, additionalScale.Output.End);
    }

    [Fact]
    public void Primary_Scale_Does_Not_Include_Named_Series_Values()
    {
        using var ctx = CreateChartContext();
        var chart = RenderChart(ctx);
        var instance = chart.Instance;

        var revenue = instance.Series.First(s => (s as IChartValueAxisSeries)?.ValueAxisName == null);
        var count = instance.Series.First(s => (s as IChartValueAxisSeries)?.ValueAxisName == "count");

        var revenueX = revenue.GetTooltipPosition(Data[1]).X;
        var countX = count.GetTooltipPosition(Data[1]).X;

        Assert.True(countX > revenueX * 0.8, $"Expected the count bar (value 358) to scale on its own axis near the revenue bar (value 269000) but got countX={countX}, revenueX={revenueX}");
    }

    [Fact]
    public void Named_Axis_Renders_Horizontally_On_Top_With_Value_Labels()
    {
        using var ctx = CreateChartContext();
        var chart = RenderChart(ctx);

        Assert.Empty(chart.FindAll("g.rz-value-axis-right"));

        var topAxisLabels = chart.FindAll("g.rz-value-axis-top .rz-tick-text")
            .Select(t => t.TextContent.Trim())
            .Where(t => !string.IsNullOrEmpty(t))
            .ToList();

        Assert.NotEmpty(topAxisLabels);
        Assert.All(topAxisLabels, label => Assert.True(double.TryParse(label, out _), $"Expected numeric tick label but got '{label}'"));

        var categories = Data.Select(d => d.Month);
        Assert.DoesNotContain(topAxisLabels, label => categories.Contains(label));
    }

    [Fact]
    public void Shared_Category_Axis_Contains_All_Categories()
    {
        using var ctx = CreateChartContext();
        var chart = RenderChart(ctx);
        var instance = chart.Instance;

        Assert.IsType<OrdinalScale>(instance.ValueScale);

        var leftAxisLabels = chart.FindAll("g.rz-value-axis:not(.rz-value-axis-top) .rz-tick-text")
            .Select(t => t.TextContent.Trim())
            .ToList();

        foreach (var month in Data.Select(d => d.Month))
        {
            Assert.Contains(month, leftAxisLabels);
        }
    }

    [Fact]
    public void Named_Axis_Tooltip_Formats_With_Named_Axis()
    {
        using var ctx = CreateChartContext();

        var chart = ctx.RenderComponent<RadzenChart>(parameters => parameters
            .AddChildContent<RadzenBarSeries<Item>>(series => series
                .Add(p => p.Title, "Revenue")
                .Add(p => p.CategoryProperty, nameof(Item.Month))
                .Add(p => p.ValueProperty, nameof(Item.Revenue))
                .Add(p => p.Data, Data))
            .AddChildContent<RadzenBarSeries<Item>>(series => series
                .Add(p => p.Title, "Order Count")
                .Add(p => p.CategoryProperty, nameof(Item.Month))
                .Add(p => p.ValueProperty, nameof(Item.Count))
                .Add(p => p.ValueAxisName, "count")
                .Add(p => p.Data, Data))
            .AddChildContent<RadzenValueAxis>(a => a
                .Add(p => p.Name, "count")
                .Add(p => p.FormatString, "{0} orders")));

        var count = chart.Instance.Series.First(s => (s as IChartValueAxisSeries)?.ValueAxisName == "count");
        var dataLabels = count.GetDataLabels(0, 0).ToList();

        Assert.NotEmpty(dataLabels);
        Assert.All(dataLabels, label => Assert.EndsWith("orders", label.Text));
    }

    [Fact]
    public void Column_Series_Multiple_Axes_Still_Render_On_Right()
    {
        using var ctx = CreateChartContext();

        var chart = ctx.RenderComponent<RadzenChart>(parameters => parameters
            .AddChildContent<RadzenColumnSeries<Item>>(series => series
                .Add(p => p.CategoryProperty, nameof(Item.Month))
                .Add(p => p.ValueProperty, nameof(Item.Revenue))
                .Add(p => p.Data, Data))
            .AddChildContent<RadzenColumnSeries<Item>>(series => series
                .Add(p => p.CategoryProperty, nameof(Item.Month))
                .Add(p => p.ValueProperty, nameof(Item.Count))
                .Add(p => p.ValueAxisName, "count")
                .Add(p => p.Data, Data))
            .AddChildContent<RadzenValueAxis>(a => a
                .Add(p => p.Name, "count")));

        Assert.NotEmpty(chart.FindAll("g.rz-value-axis-right"));
        Assert.Empty(chart.FindAll("g.rz-value-axis-top"));

        var additionalScale = chart.Instance.GetValueScale("count");
        Assert.Equal(chart.Instance.ValueScale.Output.Start, additionalScale.Output.Start);
        Assert.Equal(chart.Instance.ValueScale.Output.End, additionalScale.Output.End);
    }
}
