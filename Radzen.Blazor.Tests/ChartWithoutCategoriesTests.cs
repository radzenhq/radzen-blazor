using System.Collections.Generic;
using System.Threading.Tasks;
using Bunit;
using Xunit;
using static Radzen.Blazor.Tests.ChartTestHelper;

namespace Radzen.Blazor.Tests
{
    public class ChartWithoutCategoriesTests
    {
        static void AddSeries(ComponentParameterCollectionBuilder<RadzenChart> chart, string series, IEnumerable<DataItem> data)
        {
            switch (series)
            {
                case "Area":
                    chart.AddChildContent<RadzenAreaSeries<DataItem>>(s => s
                        .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                        .Add(x => x.ValueProperty, nameof(DataItem.Value))
                        .Add(x => x.FillMode, FillMode.Gradient)
                        .Add(x => x.Interpolation, Interpolation.Spline)
                        .Add(x => x.Data, data));
                    break;
                case "Line":
                    chart.AddChildContent<RadzenLineSeries<DataItem>>(s => s
                        .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                        .Add(x => x.ValueProperty, nameof(DataItem.Value))
                        .Add(x => x.Data, data));
                    break;
                case "Bar":
                    chart.AddChildContent<RadzenBarSeries<DataItem>>(s => s
                        .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                        .Add(x => x.ValueProperty, nameof(DataItem.Value))
                        .Add(x => x.FillMode, FillMode.Gradient)
                        .Add(x => x.Data, data));
                    break;
                default:
                    chart.AddChildContent<RadzenColumnSeries<DataItem>>(s => s
                        .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                        .Add(x => x.ValueProperty, nameof(DataItem.Value))
                        .Add(x => x.FillMode, FillMode.Gradient)
                        .Add(x => x.Data, data));
                    break;
            }
        }

        static IRenderedComponent<RadzenChart> RenderChart(TestContext ctx, string series, IEnumerable<DataItem> data)
        {
            return ctx.RenderComponent<RadzenChart>(p =>
            {
                p.Add(x => x.ColorScheme, ColorScheme.Palette)
                    .Add(x => x.Animate, true)
                    .AddChildContent<RadzenChartTooltipOptions>(t => t.Add(x => x.Shared, true))
                    .AddChildContent<RadzenLegend>(l => l.Add(x => x.Position, LegendPosition.Bottom))
                    .AddChildContent<RadzenCategoryAxis>(axis => axis
                        .AddChildContent<RadzenAxisCrosshair>(c => c
                            .Add(x => x.Visible, true)
                            .Add(x => x.Label, true)))
                    .AddChildContent<RadzenValueAxis>(axis => axis
                        .Add(x => x.Formatter, value => $"{value:N0}")
                        .AddChildContent<RadzenGridLines>(g => g.Add(x => x.Visible, true)));

                AddSeries(p, series, data);
            });
        }

        [Theory]
        [InlineData("Column", false)]
        [InlineData("Column", true)]
        [InlineData("Area", false)]
        [InlineData("Area", true)]
        [InlineData("Line", false)]
        [InlineData("Line", true)]
        [InlineData("Bar", false)]
        [InlineData("Bar", true)]
        public async Task StringCategoryChart_WithNoItems_RendersNoNaNCoordinates(string series, bool nullData)
        {
            using var ctx = CreateChartContext();

            var chart = RenderChart(ctx, series, nullData ? null : new List<DataItem>());

            await chart.InvokeAsync(() => chart.Instance.Resize(400, 320));

            Assert.Contains("rz-category-axis", chart.Markup);
            Assert.DoesNotContain("NaN", chart.Markup);

            await chart.InvokeAsync(() => chart.Instance.MouseMove(150, 100));

            Assert.Contains("rz-chart-crosshair", chart.Markup);
            Assert.DoesNotContain("NaN", chart.Markup);
        }

        [Fact]
        public async Task StringCategoryChart_WithNoItems_SpansThePlotWithOneEmptyBand()
        {
            using var ctx = CreateChartContext();

            var chart = RenderChart(ctx, "Column", new List<DataItem>());

            await chart.InvokeAsync(() => chart.Instance.Resize(400, 320));

            var scale = Assert.IsType<OrdinalScale>(chart.Instance.CategoryScale);
            Assert.Equal(-0.5, scale.Input.Start);
            Assert.Equal(0.5, scale.Input.End);
            Assert.Equal(0, scale.Scale(-0.5, false));
            Assert.Equal(scale.OutputSize, scale.Scale(0.5, false));
        }
    }
}
