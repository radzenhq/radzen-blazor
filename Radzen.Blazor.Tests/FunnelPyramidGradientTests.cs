using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Bunit;
using Xunit;
using static Radzen.Blazor.Tests.ChartTestHelper;

namespace Radzen.Blazor.Tests
{
    public class FunnelPyramidGradientTests
    {
        static int LinearGradients(string markup) => Regex.Matches(markup, "<linearGradient").Count;

        static async Task<string> RenderFunnel(TestContext ctx, FillMode? fillMode = null, string[]? fills = null)
        {
            var chart = ctx.RenderComponent<RadzenChart>(p =>
                p.AddChildContent<RadzenFunnelSeries<DataItem>>(s =>
                {
                    s.Add(x => x.CategoryProperty, nameof(DataItem.Category));
                    s.Add(x => x.ValueProperty, nameof(DataItem.Value));
                    if (fillMode.HasValue)
                    {
                        s.Add(x => x.FillMode, fillMode.Value);
                    }
                    if (fills != null)
                    {
                        s.Add(x => x.Fills, fills);
                    }
                    s.Add(x => x.Data, SampleData);
                }));

            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));
            return chart.Markup;
        }

        static async Task<string> RenderPyramid(TestContext ctx, FillMode? fillMode = null, string[]? fills = null)
        {
            var chart = ctx.RenderComponent<RadzenChart>(p =>
                p.AddChildContent<RadzenPyramidSeries<DataItem>>(s =>
                {
                    s.Add(x => x.CategoryProperty, nameof(DataItem.Category));
                    s.Add(x => x.ValueProperty, nameof(DataItem.Value));
                    if (fillMode.HasValue)
                    {
                        s.Add(x => x.FillMode, fillMode.Value);
                    }
                    if (fills != null)
                    {
                        s.Add(x => x.Fills, fills);
                    }
                    s.Add(x => x.Data, SampleData);
                }));

            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));
            return chart.Markup;
        }

        // --- Funnel ---

        [Fact]
        public async Task Funnel_SolidByDefault_RendersNoGradient()
        {
            using var ctx = CreateChartContext();

            var markup = await RenderFunnel(ctx);

            Assert.Equal(0, LinearGradients(markup));
            Assert.DoesNotContain("url(#rzFunnelGradient", markup);
        }

        [Fact]
        public async Task Funnel_Gradient_EmitsHorizontalGradientPerSegment()
        {
            using var ctx = CreateChartContext();

            var markup = await RenderFunnel(ctx, FillMode.Gradient);

            // One gradient per segment (SampleData has 3 items).
            Assert.Equal(SampleData.Length, LinearGradients(markup));
            Assert.Contains("url(#rzFunnelGradient", markup);
            Assert.Matches("<linearGradient[^>]*x2=\"1\"[^>]*y2=\"0\"", markup);
            Assert.Contains("stop-opacity: 0.55", markup);
            Assert.Contains("stop-opacity: 1\"", markup);
        }

        [Fact]
        public async Task Funnel_Gradient_NullFills_UseSeriesColorVariable()
        {
            using var ctx = CreateChartContext();

            var markup = await RenderFunnel(ctx, FillMode.Gradient);

            Assert.Contains("stop-color: var(--rz-series-color)", markup);
            Assert.DoesNotContain("stop-color: ;", markup);
        }

        [Fact]
        public async Task Funnel_Gradient_ExplicitFills_UseThatColor()
        {
            using var ctx = CreateChartContext();

            var markup = await RenderFunnel(ctx, FillMode.Gradient, fills: new[] { "#ff0000", "#00ff00", "#0000ff" });

            Assert.Contains("stop-color: #ff0000", markup);
        }

        [Fact]
        public async Task Funnel_None_RendersNoGradientAndNoFill()
        {
            using var ctx = CreateChartContext();

            var markup = await RenderFunnel(ctx, FillMode.None);

            Assert.Equal(0, LinearGradients(markup));
            Assert.Contains("fill: none", markup);
        }

        [Fact]
        public async Task Funnel_Solid_RendersNoGradient()
        {
            using var ctx = CreateChartContext();

            var markup = await RenderFunnel(ctx, FillMode.Solid);

            Assert.Equal(0, LinearGradients(markup));
        }

        // --- Pyramid ---

        [Fact]
        public async Task Pyramid_SolidByDefault_RendersNoGradient()
        {
            using var ctx = CreateChartContext();

            var markup = await RenderPyramid(ctx);

            Assert.Equal(0, LinearGradients(markup));
            Assert.DoesNotContain("url(#rzPyramidGradient", markup);
        }

        [Fact]
        public async Task Pyramid_Gradient_EmitsHorizontalGradientPerSegment()
        {
            using var ctx = CreateChartContext();

            var markup = await RenderPyramid(ctx, FillMode.Gradient);

            Assert.Equal(SampleData.Length, LinearGradients(markup));
            Assert.Contains("url(#rzPyramidGradient", markup);
            Assert.Matches("<linearGradient[^>]*x2=\"1\"[^>]*y2=\"0\"", markup);
            Assert.Contains("stop-opacity: 0.55", markup);
            Assert.Contains("stop-opacity: 1\"", markup);
        }

        [Fact]
        public async Task Pyramid_Gradient_NullFills_UseSeriesColorVariable()
        {
            using var ctx = CreateChartContext();

            var markup = await RenderPyramid(ctx, FillMode.Gradient);

            Assert.Contains("stop-color: var(--rz-series-color)", markup);
            Assert.DoesNotContain("stop-color: ;", markup);
        }

        [Fact]
        public async Task Pyramid_None_RendersNoGradientAndNoFill()
        {
            using var ctx = CreateChartContext();

            var markup = await RenderPyramid(ctx, FillMode.None);

            Assert.Equal(0, LinearGradients(markup));
            Assert.Contains("fill: none", markup);
        }
    }
}
