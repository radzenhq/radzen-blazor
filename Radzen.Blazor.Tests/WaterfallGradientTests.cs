using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Bunit;
using Xunit;
using static Radzen.Blazor.Tests.ChartTestHelper;

namespace Radzen.Blazor.Tests
{
    public class WaterfallGradientTests
    {
        static int Gradients(string markup) => Regex.Matches(markup, "<linearGradient").Count;

        // Summary start (+1), increase (+1), decrease (-1) -> two distinct (color, sign) gradients.
        static DataItem[] MixedData => new[]
        {
            new DataItem { Category = "Start", Value = 100, IsSummary = true },
            new DataItem { Category = "Up", Value = 30 },
            new DataItem { Category = "Down", Value = -10 },
        };

        static async Task<string> RenderWaterfall(TestContext ctx, FillMode? fillMode = null, DataItem[]? data = null)
        {
            var chart = ctx.RenderComponent<RadzenChart>(p =>
                p.AddChildContent<RadzenWaterfallSeries<DataItem>>(s =>
                {
                    s.Add(x => x.CategoryProperty, nameof(DataItem.Category));
                    s.Add(x => x.ValueProperty, nameof(DataItem.Value));
                    s.Add(x => x.SummaryProperty, nameof(DataItem.IsSummary));
                    if (fillMode.HasValue)
                    {
                        s.Add(x => x.FillMode, fillMode.Value);
                    }
                    s.Add(x => x.Data, data ?? MixedData);
                }));

            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));

            return chart.Markup;
        }

        static async Task<string> RenderHorizontalWaterfall(TestContext ctx, FillMode? fillMode = null, DataItem[]? data = null)
        {
            var chart = ctx.RenderComponent<RadzenChart>(p =>
                p.AddChildContent<RadzenHorizontalWaterfallSeries<DataItem>>(s =>
                {
                    s.Add(x => x.CategoryProperty, nameof(DataItem.Category));
                    s.Add(x => x.ValueProperty, nameof(DataItem.Value));
                    s.Add(x => x.SummaryProperty, nameof(DataItem.IsSummary));
                    if (fillMode.HasValue)
                    {
                        s.Add(x => x.FillMode, fillMode.Value);
                    }
                    s.Add(x => x.Data, data ?? MixedData);
                }));

            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));

            return chart.Markup;
        }

        [Fact]
        public async Task Waterfall_SolidByDefault_RendersNoGradient()
        {
            using var ctx = CreateChartContext();

            var markup = await RenderWaterfall(ctx);

            Assert.Equal(0, Gradients(markup));
            Assert.DoesNotContain("url(#rzFillGradient", markup);
        }

        [Fact]
        public async Task Waterfall_RendersGradient_WhenEnabled()
        {
            using var ctx = CreateChartContext();

            var markup = await RenderWaterfall(ctx, FillMode.Gradient);

            Assert.True(Gradients(markup) >= 1);
            Assert.Contains("url(#rzFillGradient", markup);
        }

        [Fact]
        public async Task Waterfall_GradientIsVertical()
        {
            using var ctx = CreateChartContext();

            var markup = await RenderWaterfall(ctx, FillMode.Gradient);

            Assert.Matches("<linearGradient[^>]*x2=\"0\"[^>]*y2=\"1\"", markup);
        }

        [Fact]
        public async Task HorizontalWaterfall_GradientIsHorizontal()
        {
            using var ctx = CreateChartContext();

            var markup = await RenderHorizontalWaterfall(ctx, FillMode.Gradient);

            Assert.Matches("<linearGradient[^>]*x2=\"1\"[^>]*y2=\"0\"", markup);
        }

        [Fact]
        public async Task Waterfall_MixedSigns_ProduceTwoGradients()
        {
            using var ctx = CreateChartContext();

            // Default (null) fills resolve to the series color; increase/summary are sign +1 and the
            // decrease is sign -1, so the baseline-facing stop differs -> two definitions.
            var markup = await RenderWaterfall(ctx, FillMode.Gradient);

            Assert.Equal(2, Gradients(markup));
        }

        [Fact]
        public async Task Waterfall_SameSignSameColor_ReusesOneGradient()
        {
            using var ctx = CreateChartContext();

            // Summary start (+1) and an increase (+1), both with the default series color -> one gradient.
            var data = new[]
            {
                new DataItem { Category = "Start", Value = 100, IsSummary = true },
                new DataItem { Category = "Up", Value = 30 },
            };

            var markup = await RenderWaterfall(ctx, FillMode.Gradient, data);

            Assert.Equal(1, Gradients(markup));
        }

        [Fact]
        public async Task Waterfall_PositiveGradient_IsFullAtTip()
        {
            using var ctx = CreateChartContext();

            // A single increasing bar: offset 0 (top, the value tip) carries the full start opacity.
            var data = new[] { new DataItem { Category = "Up", Value = 10 } };

            var markup = await RenderWaterfall(ctx, FillMode.Gradient, data);

            Assert.Matches("offset=\"0\"[^>]*stop-opacity: 0.85", markup);
            Assert.Matches("offset=\"1\"[^>]*stop-opacity: 0.4", markup);
        }

        [Fact]
        public async Task Waterfall_NullFill_GradientUsesSeriesColor()
        {
            using var ctx = CreateChartContext();

            var markup = await RenderWaterfall(ctx, FillMode.Gradient);

            Assert.Contains("stop-color: var(--rz-series-color)", markup);
            Assert.DoesNotContain("stop-color: ;", markup);
        }

        [Fact]
        public async Task Waterfall_None_FillsNone()
        {
            using var ctx = CreateChartContext();

            var markup = await RenderWaterfall(ctx, FillMode.None);

            Assert.Equal(0, Gradients(markup));
            Assert.Contains("fill: none", markup);
        }

        [Fact]
        public async Task Waterfall_Solid_RendersNoGradient()
        {
            using var ctx = CreateChartContext();

            var markup = await RenderWaterfall(ctx, FillMode.Solid);

            Assert.Equal(0, Gradients(markup));
        }
    }
}
