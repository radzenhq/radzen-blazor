using System.Collections.Generic;
using System.Threading.Tasks;
using Bunit;
using Xunit;
using static Radzen.Blazor.Tests.ChartTestHelper;

namespace Radzen.Blazor.Tests
{
    public class CategoryPlacementTests
    {
        static OrdinalScale OrdinalWith(int count, TickPlacement placement)
        {
            var data = new List<object>();
            for (var i = 0; i < count; i++)
            {
                data.Add($"C{i}");
            }

            return new OrdinalScale { Data = data, Placement = placement };
        }

        // --- OrdinalScale domain (Ticks) and labels (TickValues) ---

        [Fact]
        public void Between_ReservesHalfABandEachSide()
        {
            var scale = OrdinalWith(12, TickPlacement.Between);

            Assert.Equal((-0.5, 11.5, 1.0), scale.Ticks(100));
        }

        [Fact]
        public void On_IsFlush()
        {
            var scale = OrdinalWith(12, TickPlacement.On);

            Assert.Equal((0.0, 11.0, 1.0), scale.Ticks(100));
        }

        [Fact]
        public void On_SinglePoint_FallsBackToHalfBand()
        {
            // Flush on a single category would collapse the input to zero width; keep a half-band domain.
            var scale = OrdinalWith(1, TickPlacement.On);

            Assert.Equal((-0.5, 0.5, 1.0), scale.Ticks(100));
        }

        [Theory]
        [InlineData(TickPlacement.Between)]
        [InlineData(TickPlacement.On)]
        public void TickValues_AlwaysIntegerCategories(TickPlacement placement)
        {
            var scale = OrdinalWith(4, placement);

            Assert.Equal(new[] { 0.0, 1.0, 2.0, 3.0 }, scale.TickValues(100));
        }

        // --- Chart-level: vertical-category (column) routes placement to CategoryScale ---

        static async Task<RadzenChart> RenderColumn(TestContext ctx, TickPlacement placement)
        {
            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenCategoryAxis>(axis => axis.Add(x => x.TickPlacement, placement))
                .AddChildContent<RadzenColumnSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Data, SampleData)));

            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));
            return chart.Instance;
        }

        [Fact]
        public async Task Column_Between_OrdinalIsCategoryScale_HalfBandDomain()
        {
            using var ctx = CreateChartContext();

            var instance = await RenderColumn(ctx, TickPlacement.Between);
            var scale = Assert.IsType<OrdinalScale>(instance.CategoryScale);

            Assert.Equal(TickPlacement.Between, scale.Placement);
            Assert.Equal(-0.5, scale.Input.Start);
            Assert.Equal(SampleData.Length - 0.5, scale.Input.End);
        }

        [Fact]
        public async Task Column_On_IsFlush()
        {
            using var ctx = CreateChartContext();

            var instance = await RenderColumn(ctx, TickPlacement.On);
            var scale = Assert.IsType<OrdinalScale>(instance.CategoryScale);

            Assert.Equal(0, scale.Input.Start);
            Assert.Equal(SampleData.Length - 1, scale.Input.End);
        }

        // --- Chart-level: bar routes placement to ValueScale (axes swapped) ---

        static async Task<RadzenChart> RenderBar(TestContext ctx, TickPlacement placement)
        {
            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenCategoryAxis>(axis => axis.Add(x => x.TickPlacement, placement))
                .AddChildContent<RadzenBarSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Data, SampleData)));

            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));
            return chart.Instance;
        }

        [Fact]
        public async Task Bar_OrdinalIsValueScale_PlacementApplied()
        {
            using var ctx = CreateChartContext();

            // Inverted axes: the ordinal scale is the ValueScale, not the CategoryScale.
            var instance = await RenderBar(ctx, TickPlacement.Between);
            var scale = Assert.IsType<OrdinalScale>(instance.ValueScale);

            Assert.Equal(TickPlacement.Between, scale.Placement);
            Assert.Equal(-0.5, scale.Input.Start);
            Assert.Equal(SampleData.Length - 0.5, scale.Input.End);
        }

        [Theory]
        [InlineData(TickPlacement.Between)]
        [InlineData(TickPlacement.On)]
        public async Task Bar_CategoryLabels_AllRenderExactlyOnce(TickPlacement placement)
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenCategoryAxis>(axis => axis.Add(x => x.TickPlacement, placement))
                .AddChildContent<RadzenBarSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Data, SampleData)));

            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));

            // The vertical category axis must show each category once (no half-band rounding to dupes).
            foreach (var category in new[] { "A", "B", "C" })
            {
                Assert.Equal(1, System.Text.RegularExpressions.Regex.Matches(chart.Markup, $">{category}</text>").Count);
            }
        }

        // --- Bullet: ordinal on ValueScale even though the chart is not inverted ---

        [Fact]
        public async Task Bullet_PlacementApplied_OnValueScale()
        {
            using var ctx = CreateChartContext();

            var data = new[]
            {
                new DataItem { Category = "A", Value = 20, Target = 25, Max = 40 },
                new DataItem { Category = "B", Value = 18, Target = 22, Max = 40 },
            };

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenCategoryAxis>(axis => axis.Add(x => x.TickPlacement, TickPlacement.On))
                .AddChildContent<RadzenBulletSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.TargetProperty, nameof(DataItem.Target))
                    .Add(x => x.MaxProperty, nameof(DataItem.Max))
                    .Add(x => x.Data, data)));

            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));

            var scale = Assert.IsType<OrdinalScale>(chart.Instance.ValueScale);
            Assert.Equal(TickPlacement.On, scale.Placement);
            Assert.Equal(0, scale.Input.Start);
            Assert.Equal(1, scale.Input.End);
        }
    }
}
