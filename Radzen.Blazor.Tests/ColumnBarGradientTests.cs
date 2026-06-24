using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Bunit;
using Xunit;
using static Radzen.Blazor.Tests.ChartTestHelper;

namespace Radzen.Blazor.Tests
{
    public class ColumnBarGradientTests
    {
        static int Gradients(string markup) => Regex.Matches(markup, "<linearGradient").Count;

        static async Task<string> RenderColumn(TestContext ctx,
            System.Action<ComponentParameterCollectionBuilder<RadzenColumnSeries<DataItem>>>? configure = null,
            DataItem[]? data = null,
            FillMode fillMode = FillMode.Gradient)
        {
            var chart = ctx.RenderComponent<RadzenChart>(p =>
                p.AddChildContent<RadzenColumnSeries<DataItem>>(s =>
                {
                    s.Add(x => x.CategoryProperty, nameof(DataItem.Category));
                    s.Add(x => x.ValueProperty, nameof(DataItem.Value));
                    s.Add(x => x.FillMode, fillMode);
                    s.Add(x => x.Data, data ?? SampleData);
                    configure?.Invoke(s);
                }));

            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));

            return chart.Markup;
        }

        static async Task<string> RenderBar(TestContext ctx,
            System.Action<ComponentParameterCollectionBuilder<RadzenBarSeries<DataItem>>>? configure = null,
            DataItem[]? data = null,
            FillMode fillMode = FillMode.Gradient)
        {
            var chart = ctx.RenderComponent<RadzenChart>(p =>
                p.AddChildContent<RadzenBarSeries<DataItem>>(s =>
                {
                    s.Add(x => x.CategoryProperty, nameof(DataItem.Category));
                    s.Add(x => x.ValueProperty, nameof(DataItem.Value));
                    s.Add(x => x.FillMode, fillMode);
                    s.Add(x => x.Data, data ?? SampleData);
                    configure?.Invoke(s);
                }));

            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));

            return chart.Markup;
        }

        [Fact]
        public async Task Column_SolidByDefault_RendersNoGradient()
        {
            using var ctx = CreateChartContext();

            // No FillMode set -> the library default (Solid) must not emit a gradient.
            var chart = ctx.RenderComponent<RadzenChart>(p =>
                p.AddChildContent<RadzenColumnSeries<DataItem>>(s =>
                {
                    s.Add(x => x.CategoryProperty, nameof(DataItem.Category));
                    s.Add(x => x.ValueProperty, nameof(DataItem.Value));
                    s.Add(x => x.Data, SampleData);
                }));

            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));

            Assert.Equal(0, Gradients(chart.Markup));
            Assert.DoesNotContain("url(#rzFillGradient", chart.Markup);
        }

        [Fact]
        public async Task Area_SolidByDefault_RendersNoGradient()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p =>
                p.AddChildContent<RadzenAreaSeries<DataItem>>(s =>
                {
                    s.Add(x => x.CategoryProperty, nameof(DataItem.Category));
                    s.Add(x => x.ValueProperty, nameof(DataItem.Value));
                    s.Add(x => x.Data, SampleData);
                }));

            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));

            Assert.Equal(0, Gradients(chart.Markup));
        }

        [Fact]
        public async Task Column_RendersGradient_WhenEnabled()
        {
            using var ctx = CreateChartContext();

            var markup = await RenderColumn(ctx);

            Assert.True(Gradients(markup) >= 1);
            Assert.Contains("url(#rzFillGradient", markup);
        }

        [Fact]
        public async Task Bar_RendersGradient_WhenEnabled()
        {
            using var ctx = CreateChartContext();

            var markup = await RenderBar(ctx);

            Assert.True(Gradients(markup) >= 1);
            Assert.Contains("url(#rzFillGradient", markup);
        }

        [Fact]
        public async Task Column_Solid_RendersNoGradient()
        {
            using var ctx = CreateChartContext();

            var markup = await RenderColumn(ctx, fillMode: FillMode.Solid);

            Assert.Equal(0, Gradients(markup));
            Assert.DoesNotContain("url(#rzFillGradient", markup);
        }

        [Fact]
        public async Task Column_None_FillsNone()
        {
            using var ctx = CreateChartContext();

            var markup = await RenderColumn(ctx, fillMode: FillMode.None);

            Assert.Equal(0, Gradients(markup));
            Assert.Contains("fill: none", markup);
        }

        [Fact]
        public async Task Column_DistinctFills_ProduceDistinctGradients()
        {
            using var ctx = CreateChartContext();

            var markup = await RenderColumn(ctx, s => s.Add(x => x.Fills, new[] { "red", "red", "blue" }));

            // Three positive columns, two distinct colors -> two gradient definitions.
            Assert.Equal(2, Gradients(markup));
        }

        [Fact]
        public async Task Column_SameFill_ReusesOneGradient()
        {
            using var ctx = CreateChartContext();

            var markup = await RenderColumn(ctx, s => s.Add(x => x.Fills, new[] { "red", "red", "red" }));

            Assert.Equal(1, Gradients(markup));
        }

        [Fact]
        public async Task Column_GradientIsVertical()
        {
            using var ctx = CreateChartContext();

            var markup = await RenderColumn(ctx);

            Assert.Matches("<linearGradient[^>]*x2=\"0\"[^>]*y2=\"1\"", markup);
        }

        [Fact]
        public async Task Bar_GradientIsHorizontal()
        {
            using var ctx = CreateChartContext();

            var markup = await RenderBar(ctx);

            Assert.Matches("<linearGradient[^>]*x2=\"1\"[^>]*y2=\"0\"", markup);
        }

        [Fact]
        public async Task Column_MixedSigns_ProduceTwoGradients()
        {
            using var ctx = CreateChartContext();

            var data = new[]
            {
                new DataItem { Category = "A", Value = 10 },
                new DataItem { Category = "B", Value = -10 },
            };

            // Same color but opposite signs -> the baseline-facing stop differs, so two definitions.
            var markup = await RenderColumn(ctx, data: data);

            Assert.Equal(2, Gradients(markup));
        }

        [Fact]
        public async Task Column_PositiveGradient_IsFullAtTip()
        {
            using var ctx = CreateChartContext();

            var data = new[] { new DataItem { Category = "A", Value = 10 } };

            var markup = await RenderColumn(ctx, data: data);

            // A single positive column: offset 0 (top, the value tip) carries the full start opacity.
            Assert.Matches("offset=\"0\"[^>]*stop-opacity: 0.85", markup);
            Assert.Matches("offset=\"1\"[^>]*stop-opacity: 0.4", markup);
        }

        [Fact]
        public async Task StackedColumn_EmptyFill_GradientUsesSeriesColor()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p =>
                p.AddChildContent<RadzenStackedColumnSeries<DataItem>>(s =>
                {
                    s.Add(x => x.CategoryProperty, nameof(DataItem.Category));
                    s.Add(x => x.ValueProperty, nameof(DataItem.Value));
                    s.Add(x => x.FillMode, FillMode.Gradient);
                    s.Add(x => x.Data, SampleData);
                }));

            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));

            // RadzenStackedColumnSeries.Fill defaults to string.Empty, so the gradient color must
            // fall back to the series color rather than rendering an empty (black) stop.
            Assert.Contains("stop-color: var(--rz-series-color)", chart.Markup);
            Assert.DoesNotContain("stop-color: ;", chart.Markup);
        }

        [Fact]
        public async Task Bullet_RendersGradient_WhenEnabled()
        {
            using var ctx = CreateChartContext();

            var data = new[]
            {
                new DataItem { Category = "A", Value = 20, Target = 25, Max = 40 },
            };

            var chart = ctx.RenderComponent<RadzenChart>(p =>
                p.AddChildContent<RadzenBulletSeries<DataItem>>(s =>
                {
                    s.Add(x => x.CategoryProperty, nameof(DataItem.Category));
                    s.Add(x => x.ValueProperty, nameof(DataItem.Value));
                    s.Add(x => x.TargetProperty, nameof(DataItem.Target));
                    s.Add(x => x.MaxProperty, nameof(DataItem.Max));
                    s.Add(x => x.FillMode, FillMode.Gradient);
                    s.Add(x => x.Data, data);
                }));

            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));

            Assert.True(Gradients(chart.Markup) >= 1);
            Assert.Contains("url(#rzFillGradient", chart.Markup);
        }
    }
}
