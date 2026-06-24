using Bunit;
using Xunit;
using static Radzen.Blazor.Tests.ChartTestHelper;

namespace Radzen.Blazor.Tests
{
    public class LineSeriesTests
    {
        [Fact]
        public async System.Threading.Tasks.Task LineSeries_Renders_PathWithCorrectYCoordinates()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenLineSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Stroke, "#FF0000")
                    .Add(x => x.Data, SampleData)));

            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));

            // Values: A=10(min=bottom,y=236), B=20(max=top,y=0), C=15(mid,y=118)
            Assert.Contains("rz-line-series rz-series-0", chart.Markup);
            Assert.Matches(@"M\s+[\d.]+\s+236\s+L\s+[\d.]+\s+0\s+L\s+[\d.]+\s+118", chart.Markup);
        }

        [Fact]
        public void LineSeries_StrokeColor_AppliedToPathStyle()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenLineSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Stroke, "#FF0000")
                    .Add(x => x.Data, SampleData)));

            Assert.Contains("stroke: #FF0000", chart.Markup);
            Assert.Contains("fill: none", chart.Markup);
            Assert.Contains("stroke-width: 2", chart.Markup);
        }

        [Fact]
        public async System.Threading.Tasks.Task LineSeries_ValueAxis_RendersTicksForDataRange()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenLineSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Data, SampleData)));

            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));

            Assert.Contains("rz-value-axis", chart.Markup);
            Assert.Contains(">10</text>", chart.Markup);
            Assert.Contains(">15</text>", chart.Markup);
            Assert.Contains(">20</text>", chart.Markup);
        }

        [Fact]
        public void LineSeries_CategoryAxis_RendersCategoryLabels()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenLineSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Data, SampleData)));

            Assert.Contains("rz-category-axis", chart.Markup);
            Assert.Contains(">A</text>", chart.Markup);
            Assert.Contains(">B</text>", chart.Markup);
            Assert.Contains(">C</text>", chart.Markup);
        }

        [Fact]
        public void LineSeries_Title_RendersInLegendItem()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenLineSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Title, "Revenue")
                    .Add(x => x.Data, SampleData)));

            Assert.Contains(@"class=""rz-legend-item-text""", chart.Markup);
            Assert.Contains(">Revenue</span>", chart.Markup);
        }

        [Fact]
        public void LineSeries_Hidden_NoSeriesGroupRendered()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenLineSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Visible, false)
                    .Add(x => x.Data, SampleData)));

            Assert.DoesNotContain("rz-line-series", chart.Markup);
        }

        [Fact]
        public void LineSeries_Smooth_UsesCubicBezierInPath()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenLineSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Smooth, true)
                    .Add(x => x.Data, SampleData)));

            var series = chart.FindComponent<RadzenLineSeries<DataItem>>();
            Assert.Equal(Interpolation.Spline, series.Instance.Interpolation);
            // Spline path uses C (cubic bezier) commands instead of L (line)
            Assert.Contains(" C ", chart.Markup);
        }

        [Fact]
        public void LineSeries_CustomStrokeWidth_AppliedToSvg()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenLineSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.StrokeWidth, 5)
                    .Add(x => x.Data, SampleData)));

            Assert.Contains("stroke-width: 5", chart.Markup);
        }

        [Fact]
        public async System.Threading.Tasks.Task LineSeries_NoCategoryProperty_UsesIndexForXAxis()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenLineSeries<DataItem>>(s => s
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Data, SampleData)));

            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));

            // Without CategoryProperty the X axis should use indices 0, 1, 2
            Assert.Contains(">0</text>", chart.Markup);
            Assert.Contains(">1</text>", chart.Markup);
            Assert.Contains(">2</text>", chart.Markup);

            // The line series path (inside rz-line-series group) should have three distinct, increasing X coordinates
            var seriesMatch = System.Text.RegularExpressions.Regex.Match(chart.Markup, @"rz-line-series[\s\S]*?d=""(M\s+[\d.]+\s+[\d.]+\s+L\s+[\d.]+\s+[\d.]+\s+L\s+[\d.]+\s+[\d.]+)");
            Assert.True(seriesMatch.Success, "Should find line series path with M and two L commands");
            var pathMatch = System.Text.RegularExpressions.Regex.Match(seriesMatch.Groups[1].Value, @"M\s+([\d.]+)\s+[\d.]+\s+L\s+([\d.]+)\s+[\d.]+\s+L\s+([\d.]+)\s+[\d.]+");
            var x0 = double.Parse(pathMatch.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
            var x1 = double.Parse(pathMatch.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture);
            var x2 = double.Parse(pathMatch.Groups[3].Value, System.Globalization.CultureInfo.InvariantCulture);
            Assert.True(x0 < x1 && x1 < x2, $"X coordinates should be distinct and increasing: {x0}, {x1}, {x2}");
        }

        [Fact]
        public void LineSeries_Multiple_RendersDistinctSeriesGroups()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenLineSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Title, "S1")
                    .Add(x => x.Data, SampleData))
                .AddChildContent<RadzenLineSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value2))
                    .Add(x => x.Title, "S2")
                    .Add(x => x.Data, SampleData)));

            Assert.Contains("rz-line-series rz-series-0", chart.Markup);
            Assert.Contains("rz-line-series rz-series-1", chart.Markup);
            Assert.Contains(">S1</span>", chart.Markup);
            Assert.Contains(">S2</span>", chart.Markup);
        }
    }
}
