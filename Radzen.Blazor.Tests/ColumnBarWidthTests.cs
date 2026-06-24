using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Bunit;
using Radzen.Blazor.Rendering;
using Xunit;
using static Radzen.Blazor.Tests.ChartTestHelper;

namespace Radzen.Blazor.Tests
{
    public class ColumnBarWidthTests
    {
        // --- BandLayout helper (the cap + group-centering math) ---

        [Fact]
        public void Resolve_Uncapped_GroupEqualsBand()
        {
            // No cap => identical to dividing the band evenly; the group spans the whole band, so the
            // centered position matches the legacy "- band/2 + index*(width+margin)" formula.
            var (size, group) = BandLayout.Resolve(120, 2, 10, null);

            Assert.Equal(55, size);   // 120/2 - 10 + 10/2
            Assert.Equal(120, group); // 2*55 + 10  == band
        }

        [Fact]
        public void Resolve_Capped_NarrowsAndCentersGroup()
        {
            var (size, group) = BandLayout.Resolve(120, 2, 10, 20);

            Assert.Equal(20, size);  // capped
            Assert.Equal(50, group); // 2*20 + 10  < band  => centered
        }

        [Fact]
        public void Resolve_SingleSeries_Uncapped_FillsBand()
        {
            var (size, group) = BandLayout.Resolve(100, 1, 10, null);

            Assert.Equal(100, size);
            Assert.Equal(100, group);
        }

        [Fact]
        public void Resolve_ZeroSeries_TreatedAsOne()
        {
            var (size, group) = BandLayout.Resolve(80, 0, 10, null);

            Assert.Equal(80, size);
            Assert.Equal(80, group);
        }

        // --- EffectiveMax precedence (Width/Height wins over the cap) ---

        [Fact]
        public void EffectiveMaxWidth_IgnoredWhenExplicitWidthSet()
        {
            Assert.Equal(40, new RadzenColumnOptions { MaxWidth = 40 }.EffectiveMaxWidth);
            Assert.Null(new RadzenColumnOptions { Width = 30, MaxWidth = 40 }.EffectiveMaxWidth);
        }

        [Fact]
        public void EffectiveMaxHeight_IgnoredWhenExplicitHeightSet()
        {
            Assert.Equal(40, new RadzenBarOptions { MaxHeight = 40 }.EffectiveMaxHeight);
            Assert.Null(new RadzenBarOptions { Height = 30, MaxHeight = 40 }.EffectiveMaxHeight);
        }

        // --- Chart-level: column width geometry ---

        static double FirstColumnWidth(string markup)
        {
            var path = Regex.Match(markup, "rz-column-series[^>]*>.*?<path[^>]*d=\"(?<d>[^\"]+)\"",
                RegexOptions.Singleline).Groups["d"].Value;
            // Positive column path ends with: L {x+width} {y0} L {x} {y0} Z
            var m = Regex.Match(path, @"L (?<xr>[-\d.]+) [-\d.]+ L (?<xl>[-\d.]+) [-\d.]+ Z");
            return Math.Abs(double.Parse(m.Groups["xr"].Value, System.Globalization.CultureInfo.InvariantCulture)
                          - double.Parse(m.Groups["xl"].Value, System.Globalization.CultureInfo.InvariantCulture));
        }

        static async Task<IRenderedComponent<RadzenChart>> RenderColumn(TestContext ctx,
            Action<ComponentParameterCollectionBuilder<RadzenColumnOptions>>? options = null)
        {
            var chart = ctx.RenderComponent<RadzenChart>(p =>
            {
                p.AddChildContent<RadzenColumnSeries<DataItem>>(s =>
                {
                    s.Add(x => x.CategoryProperty, nameof(DataItem.Category));
                    s.Add(x => x.ValueProperty, nameof(DataItem.Value));
                    s.Add(x => x.Data, SampleData);
                });
                if (options != null)
                {
                    p.AddChildContent<RadzenColumnOptions>(options);
                }
            });

            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));
            return chart;
        }

        [Fact]
        public async Task Column_MaxWidth_CapsTheWidth()
        {
            using var ctx = CreateChartContext();

            var chart = await RenderColumn(ctx, o => o.Add(x => x.MaxWidth, 20));

            Assert.Equal(20, FirstColumnWidth(chart.Markup), 1);
        }

        [Fact]
        public async Task Column_MaxWidthAboveAuto_IsNoOp()
        {
            using var ctx = CreateChartContext();

            var baseline = FirstColumnWidth((await RenderColumn(ctx)).Markup);
            var huge = FirstColumnWidth((await RenderColumn(ctx, o => o.Add(x => x.MaxWidth, 10000))).Markup);

            Assert.Equal(baseline, huge, 3);
        }

        [Fact]
        public async Task Column_CategoryGap_SetsBandToFractionOfStep()
        {
            using var ctx = CreateChartContext();

            var chart = await RenderColumn(ctx, o => o.Add(x => x.CategoryGap, 0.5));

            var scale = chart.Instance.CategoryScale;
            var step = Math.Abs(scale.Scale(1, true) - scale.Scale(0, true));

            // Single series: the column fills the whole band = step * (1 - gap).
            Assert.Equal(step * 0.5, FirstColumnWidth(chart.Markup), 1);
        }

        [Fact]
        public async Task Column_CategoryGap_ChangesRendering()
        {
            using var ctx = CreateChartContext();

            var baseline = FirstColumnWidth((await RenderColumn(ctx)).Markup);
            var gapped = FirstColumnWidth((await RenderColumn(ctx, o => o.Add(x => x.CategoryGap, 0.5))).Markup);

            Assert.True(Math.Abs(baseline - gapped) > 1, $"expected different widths, got {baseline} and {gapped}");
        }

        // --- Chart-level: bar height geometry ---

        [Fact]
        public async Task Bar_MaxHeight_CapsTheHeight()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p =>
            {
                p.AddChildContent<RadzenBarSeries<DataItem>>(s =>
                {
                    s.Add(x => x.CategoryProperty, nameof(DataItem.Category));
                    s.Add(x => x.ValueProperty, nameof(DataItem.Value));
                    s.Add(x => x.Data, SampleData);
                });
                p.AddChildContent<RadzenBarOptions>(o => o.Add(x => x.MaxHeight, 20));
            });

            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));

            // Bar path: starts "M {x0} {top} ..." and closes "L {x0} {bottom} Z".
            var path = Regex.Match(chart.Markup, "rz-bar-series[^>]*>.*?<path[^>]*d=\"(?<d>[^\"]+)\"",
                RegexOptions.Singleline).Groups["d"].Value;
            var top = double.Parse(Regex.Match(path, @"^M [-\d.]+ (?<top>[-\d.]+)").Groups["top"].Value, System.Globalization.CultureInfo.InvariantCulture);
            var bottom = double.Parse(Regex.Match(path, @"L [-\d.]+ (?<bot>[-\d.]+) Z").Groups["bot"].Value, System.Globalization.CultureInfo.InvariantCulture);
            var height = Math.Abs(bottom - top);

            Assert.Equal(20, height, 1);
        }
    }
}
