using Bunit;
using Xunit;
using static Radzen.Blazor.Tests.ChartTestHelper;

namespace Radzen.Blazor.Tests
{
    public class BulletSeriesTests
    {
        private static DataItem[] BulletData => new[]
        {
            new DataItem { Category = "Revenue", Value = 270, Target = 300, Max = 400 },
            new DataItem { Category = "Profit", Value = 23, Target = 26, Max = 40 },
        };

        [Fact]
        public void BulletSeries_Renders_BulletClass()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenBulletSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.TargetProperty, nameof(DataItem.Target))
                    .Add(x => x.MaxProperty, nameof(DataItem.Max))
                    .Add(x => x.Data, BulletData)));

            Assert.Contains("rz-bullet-series", chart.Markup);
        }

        [Fact]
        public void BulletSeries_CustomFill_Applied()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenBulletSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.TargetProperty, nameof(DataItem.Target))
                    .Add(x => x.MaxProperty, nameof(DataItem.Max))
                    .Add(x => x.Fill, "#336699")
                    .Add(x => x.Data, BulletData)));

            Assert.Contains("#336699", chart.Markup);
        }

        [Fact]
        public void BulletSeries_Hidden_DoesNotRender()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenBulletSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.TargetProperty, nameof(DataItem.Target))
                    .Add(x => x.MaxProperty, nameof(DataItem.Max))
                    .Add(x => x.Visible, false)
                    .Add(x => x.Data, BulletData)));

            Assert.DoesNotContain("rz-bullet-series", chart.Markup);
        }
    }
}
