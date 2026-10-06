using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Bunit;
using Xunit;
using static Radzen.Blazor.Tests.ChartTestHelper;

namespace Radzen.Blazor.Tests
{
    public class ChartZoomRenderCullingTests
    {
        static readonly DataItem[] Months = new[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" }
            .Select((month, index) => new DataItem { Category = month, Value = 10 + index })
            .ToArray();

        static readonly string[] Fills = Months.Select((_, index) => $"#0000{index:X2}").ToArray();

        static async Task<IRenderedComponent<RadzenChart>> RenderColumnChart(TestContext ctx, double viewStart, double viewEnd)
        {
            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .Add(x => x.AllowZoom, true)
                .Add(x => x.ViewStart, viewStart)
                .Add(x => x.ViewEnd, viewEnd)
                .AddChildContent<RadzenColumnSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Fills, Fills)
                    .Add(x => x.Data, Months)));

            await chart.InvokeAsync(() => chart.Instance.Resize(600, 300));

            return chart;
        }

        static async Task<IRenderedComponent<RadzenChart>> RenderLineChart(TestContext ctx, double viewStart, double viewEnd, bool animate = false, Interpolation interpolation = Interpolation.Line)
        {
            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .Add(x => x.AllowZoom, true)
                .Add(x => x.AnimateDataUpdates, animate)
                .Add(x => x.ViewStart, viewStart)
                .Add(x => x.ViewEnd, viewEnd)
                .AddChildContent<RadzenLineSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Interpolation, interpolation)
                    .Add(x => x.Data, Months)));

            await chart.InvokeAsync(() => chart.Instance.Resize(600, 300));

            return chart;
        }

        static int LinePointCount(IRenderedComponent<RadzenChart> chart)
        {
            var d = chart.Find(".rz-line-series path").GetAttribute("d")!;

            return Regex.Matches(d, "L ").Count + 1;
        }

        static string? FillOf(AngleSharp.Dom.IElement path)
        {
            return Regex.Match(path.GetAttribute("style") ?? "", @"fill: (#[0-9A-Fa-f]+)").Groups[1].Value;
        }

        static double StartX(AngleSharp.Dom.IElement path)
        {
            return double.Parse(path.GetAttribute("d")!.Split(' ')[1], CultureInfo.InvariantCulture);
        }

        [Fact]
        public async Task ColumnSeries_UnzoomedChart_RendersEveryColumn()
        {
            using var ctx = CreateChartContext();

            var chart = await RenderColumnChart(ctx, 0, 1);

            Assert.Equal(Months.Length, chart.FindAll(".rz-column-series path").Count);
        }

        [Fact]
        public async Task ColumnSeries_ZoomedChart_RendersOnlyColumnsThatReachTheView()
        {
            using var ctx = CreateChartContext();

            var chart = await RenderColumnChart(ctx, 0.55, 1);

            var paths = chart.FindAll(".rz-column-series path");

            Assert.Equal(6, paths.Count);
            Assert.Equal(Fills.Skip(6), paths.Select(FillOf));
            Assert.True(StartX(paths[0]) < 0, "the straddling Jul column starts left of the plot area");
            Assert.All(paths.Skip(1), path => Assert.True(StartX(path) >= 0));
        }

        [Fact]
        public async Task ColumnSeries_ZoomedChart_WithoutFillsKeepsSeriesFill()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .Add(x => x.AllowZoom, true)
                .Add(x => x.ViewStart, 0.55)
                .Add(x => x.ViewEnd, 1)
                .AddChildContent<RadzenColumnSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Fill, "#336699")
                    .Add(x => x.Data, Months)));

            await chart.InvokeAsync(() => chart.Instance.Resize(600, 300));

            Assert.Equal(6, Regex.Matches(chart.Markup, "fill: #336699").Count);
        }

        [Fact]
        public async Task LineSeries_ZoomedChart_KeepsOneNeighborBeyondTheView()
        {
            using var ctx = CreateChartContext();

            var full = await RenderLineChart(ctx, 0, 1);
            var zoomed = await RenderLineChart(ctx, 0.55, 1);

            var fullPoints = LinePointCount(full);
            var zoomedPoints = LinePointCount(zoomed);
            var visibleMarkers = zoomed.FindAll(".rz-marker").Count;

            Assert.Equal(Months.Length, fullPoints);
            Assert.InRange(visibleMarkers, 1, Months.Length - 2);
            Assert.Equal(visibleMarkers + 1, zoomedPoints);
        }

        [Fact]
        public async Task LineSeries_ZoomedSplineChart_KeepsTwoNeighborsBeyondTheView()
        {
            using var ctx = CreateChartContext();

            var zoomed = await RenderLineChart(ctx, 0.55, 1, interpolation: Interpolation.Spline);

            var visibleMarkers = zoomed.FindAll(".rz-marker").Count;
            var curves = Regex.Matches(zoomed.Find(".rz-line-series path").GetAttribute("d")!, "C ").Count;

            Assert.InRange(visibleMarkers, 1, Months.Length - 3);
            Assert.Equal(visibleMarkers + 1, curves);
        }

        [Fact]
        public async Task LineSeries_ZoomedChart_WithAnimateDataUpdates_RendersEveryPoint()
        {
            using var ctx = CreateChartContext();

            var zoomed = await RenderLineChart(ctx, 0.55, 1, animate: true);

            Assert.Equal(Months.Length, LinePointCount(zoomed));
        }

        [Fact]
        public async Task OnWheel_AppliesTheMagnitudeOfDeltaAsWheelSteps()
        {
            using var ctx = CreateChartContext();

            var single = await RenderLineChart(ctx, 0, 1);
            var batched = await RenderLineChart(ctx, 0, 1);

            await single.InvokeAsync(() => single.Instance.OnWheel(300, -1));
            await single.InvokeAsync(() => single.Instance.OnWheel(300, -1));
            await single.InvokeAsync(() => single.Instance.OnWheel(300, -1));
            await batched.InvokeAsync(() => batched.Instance.OnWheel(300, -3));

            Assert.Equal(single.Instance.ViewStart, batched.Instance.ViewStart, 10);
            Assert.Equal(single.Instance.ViewEnd, batched.Instance.ViewEnd, 10);
            Assert.True(batched.Instance.ViewEnd - batched.Instance.ViewStart < 0.6);
        }
    }
}
