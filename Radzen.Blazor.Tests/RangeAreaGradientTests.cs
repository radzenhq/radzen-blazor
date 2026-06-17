using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Bunit;
using Xunit;
using static Radzen.Blazor.Tests.ChartTestHelper;

namespace Radzen.Blazor.Tests
{
    public class RangeAreaGradientTests
    {
        static int LinearGradients(string markup) => Regex.Matches(markup, "<linearGradient").Count;

        static async Task<string> RenderRangeArea(TestContext ctx, FillMode? fillMode = null,
            string? fill = null, Interpolation interpolation = Interpolation.Line)
        {
            var chart = ctx.RenderComponent<RadzenChart>(p =>
                p.AddChildContent<RadzenRangeAreaSeries<DataItem>>(s =>
                {
                    s.Add(x => x.CategoryProperty, nameof(DataItem.Category));
                    s.Add(x => x.MinProperty, nameof(DataItem.Min));
                    s.Add(x => x.MaxProperty, nameof(DataItem.Max));
                    s.Add(x => x.Interpolation, interpolation);
                    if (fillMode.HasValue)
                    {
                        s.Add(x => x.FillMode, fillMode.Value);
                    }
                    if (fill != null)
                    {
                        s.Add(x => x.Fill, fill);
                    }
                    s.Add(x => x.Data, SampleData);
                }));

            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));

            return chart.Markup;
        }

        [Fact]
        public async Task SolidByDefault_RendersNoGradient()
        {
            using var ctx = CreateChartContext();

            var markup = await RenderRangeArea(ctx);

            Assert.Equal(0, LinearGradients(markup));
            Assert.DoesNotContain("url(#rzRangeAreaGradient", markup);
        }

        [Fact]
        public async Task Gradient_EmitsVerticalLinearGradientWithOpacityStops()
        {
            using var ctx = CreateChartContext();

            var markup = await RenderRangeArea(ctx, FillMode.Gradient);

            Assert.Equal(1, LinearGradients(markup));
            Assert.Contains("x2=\"0\" y2=\"1\"", markup);
            Assert.Contains("stop-opacity: 0.55", markup);
            Assert.Contains("stop-opacity: 0.05", markup);
            Assert.Contains("url(#rzRangeAreaGradient", markup);
        }

        [Fact]
        public async Task Gradient_NullFill_UsesSeriesColorVariable()
        {
            using var ctx = CreateChartContext();

            var markup = await RenderRangeArea(ctx, FillMode.Gradient);

            Assert.Contains("stop-color: var(--rz-series-color)", markup);
        }

        [Fact]
        public async Task Gradient_ExplicitFill_UsesThatColor()
        {
            using var ctx = CreateChartContext();

            var markup = await RenderRangeArea(ctx, FillMode.Gradient, fill: "#ff0000");

            Assert.Contains("stop-color: #ff0000", markup);
        }

        [Fact]
        public async Task Gradient_StepInterpolation_AlsoEmitsGradient()
        {
            using var ctx = CreateChartContext();

            var markup = await RenderRangeArea(ctx, FillMode.Gradient, interpolation: Interpolation.Step);

            Assert.Equal(1, LinearGradients(markup));
            Assert.Contains("url(#rzRangeAreaGradient", markup);
        }

        [Fact]
        public async Task None_RendersNoFillPathAndNoGradient()
        {
            using var ctx = CreateChartContext();

            var markup = await RenderRangeArea(ctx, FillMode.None);

            Assert.Equal(0, LinearGradients(markup));
            Assert.DoesNotContain("url(#rzRangeAreaGradient", markup);
        }

        [Fact]
        public async Task Solid_RendersNoGradient()
        {
            using var ctx = CreateChartContext();

            var markup = await RenderRangeArea(ctx, FillMode.Solid);

            Assert.Equal(0, LinearGradients(markup));
        }
    }
}
