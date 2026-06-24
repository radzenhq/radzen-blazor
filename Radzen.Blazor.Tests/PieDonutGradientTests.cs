using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Bunit;
using Xunit;
using static Radzen.Blazor.Tests.ChartTestHelper;

namespace Radzen.Blazor.Tests
{
    public class PieDonutGradientTests
    {
        static int RadialGradients(string markup) => Regex.Matches(markup, "<radialGradient").Count;

        static async Task<string> RenderPie(TestContext ctx, FillMode fillMode = FillMode.Gradient, string[]? fills = null)
        {
            var chart = ctx.RenderComponent<RadzenChart>(p =>
                p.AddChildContent<RadzenPieSeries<DataItem>>(s =>
                {
                    s.Add(x => x.CategoryProperty, nameof(DataItem.Category));
                    s.Add(x => x.ValueProperty, nameof(DataItem.Value));
                    s.Add(x => x.FillMode, fillMode);
                    if (fills != null)
                    {
                        s.Add(x => x.Fills, fills);
                    }
                    s.Add(x => x.Data, SampleData);
                }));

            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));

            return chart.Markup;
        }

        static async Task<string> RenderDonut(TestContext ctx, FillMode fillMode = FillMode.Gradient, string[]? fills = null)
        {
            var chart = ctx.RenderComponent<RadzenChart>(p =>
                p.AddChildContent<RadzenDonutSeries<DataItem>>(s =>
                {
                    s.Add(x => x.CategoryProperty, nameof(DataItem.Category));
                    s.Add(x => x.ValueProperty, nameof(DataItem.Value));
                    s.Add(x => x.FillMode, fillMode);
                    if (fills != null)
                    {
                        s.Add(x => x.Fills, fills);
                    }
                    s.Add(x => x.Data, SampleData);
                }));

            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));

            return chart.Markup;
        }

        [Fact]
        public async Task Pie_SolidByDefault_RendersNoGradient()
        {
            using var ctx = CreateChartContext();

            // No FillMode set -> the library default (Solid) must not emit a gradient.
            var chart = ctx.RenderComponent<RadzenChart>(p =>
                p.AddChildContent<RadzenPieSeries<DataItem>>(s =>
                {
                    s.Add(x => x.CategoryProperty, nameof(DataItem.Category));
                    s.Add(x => x.ValueProperty, nameof(DataItem.Value));
                    s.Add(x => x.Data, SampleData);
                }));

            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));

            Assert.Equal(0, RadialGradients(chart.Markup));
            Assert.DoesNotContain("url(#rzPieGradient", chart.Markup);
        }

        [Fact]
        public async Task Pie_Gradient_RendersRadialPerSlice()
        {
            using var ctx = CreateChartContext();

            var markup = await RenderPie(ctx, fills: new[] { "#FF0000", "#00FF00", "#0000FF" });

            // Pie uses a 2-stop radial per slice (the inverse of the donut: full color at the center,
            // faded toward the rim), referenced from its slice path.
            Assert.Equal(3, RadialGradients(markup));
            Assert.Contains("url(#rzPieGradient", markup);
            Assert.Matches("<radialGradient[^>]*gradientUnits=\"userSpaceOnUse\"", markup);
        }

        [Fact]
        public async Task Pie_Gradient_HasThreeOpacityStops()
        {
            using var ctx = CreateChartContext();

            var markup = await RenderPie(ctx, fills: new[] { "#FF0000", "#00FF00", "#0000FF" });

            // 3 slices x 3 stops each.
            Assert.Equal(9, Regex.Matches(markup, "<stop").Count);
            // Default stops: full color out to 40% of the radius (0.4), faded middle (0.9), rim (1).
            Assert.Matches("offset=\"0.4\"[^>]*stop-opacity: 1", markup);
            Assert.Matches("offset=\"0.9\"[^>]*stop-opacity: 0.62", markup);
            Assert.Matches("offset=\"1\"[^>]*stop-opacity: 0.7", markup);
            // Opacity, not a white mix - so the gradient adapts to the theme background.
            Assert.DoesNotContain("color-mix", markup);
        }

        [Fact]
        public async Task Pie_Gradient_StopShapeIsParameterized()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p =>
                p.AddChildContent<RadzenPieSeries<DataItem>>(s =>
                {
                    s.Add(x => x.CategoryProperty, nameof(DataItem.Category));
                    s.Add(x => x.ValueProperty, nameof(DataItem.Value));
                    s.Add(x => x.FillMode, FillMode.Gradient);
                    s.Add(x => x.GradientInnerOffset, 0.25);
                    s.Add(x => x.GradientMidOffset, 0.7);
                    s.Add(x => x.GradientRimOpacity, 0.8);
                    s.Add(x => x.Fills, new[] { "#FF0000", "#00FF00", "#0000FF" });
                    s.Add(x => x.Data, SampleData);
                }));

            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));

            Assert.Contains("offset=\"0.25\"", chart.Markup);
            Assert.Contains("offset=\"0.7\"", chart.Markup);
            Assert.Matches("offset=\"1\"[^>]*stop-opacity: 0.8", chart.Markup);
        }

        [Fact]
        public async Task Donut_Gradient_HasThreeOpacityStops()
        {
            using var ctx = CreateChartContext();

            var markup = await RenderDonut(ctx, fills: new[] { "#FF0000", "#00FF00", "#0000FF" });

            // The donut uses the same 3-stop radial as the pie (full color out to 0.4, faded middle at 0.9, rim at 1).
            Assert.Equal(9, Regex.Matches(markup, "<stop").Count);
            Assert.Matches("offset=\"0.4\"[^>]*stop-opacity: 1", markup);
            Assert.Matches("offset=\"0.9\"[^>]*stop-opacity: 0.62", markup);
            Assert.Matches("offset=\"1\"[^>]*stop-opacity: 0.7", markup);
            Assert.DoesNotContain("color-mix", markup);
        }

        [Fact]
        public async Task Pie_Gradient_ExplicitFills_StopColorIsSliceColor()
        {
            using var ctx = CreateChartContext();

            var markup = await RenderPie(ctx, fills: new[] { "#FF0000", "#00FF00", "#0000FF" });

            Assert.Contains("stop-color: #FF0000", markup);
            Assert.Contains("stop-color: #00FF00", markup);
            Assert.Contains("stop-color: #0000FF", markup);
        }

        [Fact]
        public async Task Pie_Gradient_NullFills_UsesSeriesColorVariable()
        {
            using var ctx = CreateChartContext();

            // Without Fills, each slice's color comes from its .rz-series-item-N CSS scope, so the
            // gradient stop must reference the series-color variable rather than an empty color.
            var markup = await RenderPie(ctx);

            Assert.True(RadialGradients(markup) >= 1);
            Assert.Contains("stop-color: var(--rz-series-color)", markup);
            Assert.DoesNotContain("stop-color: ;", markup);
        }

        [Fact]
        public async Task Pie_Solid_RendersNoGradient()
        {
            using var ctx = CreateChartContext();

            var markup = await RenderPie(ctx, fillMode: FillMode.Solid, fills: new[] { "#FF0000", "#00FF00", "#0000FF" });

            Assert.Equal(0, RadialGradients(markup));
            Assert.DoesNotContain("url(#rzPieGradient", markup);
        }

        [Fact]
        public async Task Donut_Gradient_RendersRadialPerSlice()
        {
            using var ctx = CreateChartContext();

            var markup = await RenderDonut(ctx, fills: new[] { "#FF0000", "#00FF00", "#0000FF" });

            Assert.Equal(3, RadialGradients(markup));
            Assert.Contains("url(#rzPieGradient", markup);
            Assert.Matches("<radialGradient[^>]*gradientUnits=\"userSpaceOnUse\"", markup);
        }

        [Fact]
        public async Task Donut_SolidByDefault_RendersNoGradient()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p =>
                p.AddChildContent<RadzenDonutSeries<DataItem>>(s =>
                {
                    s.Add(x => x.CategoryProperty, nameof(DataItem.Category));
                    s.Add(x => x.ValueProperty, nameof(DataItem.Value));
                    s.Add(x => x.Data, SampleData);
                }));

            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));

            Assert.Equal(0, RadialGradients(chart.Markup));
        }
    }
}
