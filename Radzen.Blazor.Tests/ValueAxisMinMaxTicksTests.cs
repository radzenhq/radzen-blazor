using System;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using Xunit;
using static Radzen.Blazor.Tests.ChartTestHelper;

namespace Radzen.Blazor.Tests
{
    public class ValueAxisMinMaxTicksTests
    {
        private static readonly DataItem[] Revenue =
        {
            new DataItem { Category = "Jan", Value = 800 },
            new DataItem { Category = "Feb", Value = 600 },
            new DataItem { Category = "Mar", Value = 1000 },
            new DataItem { Category = "Apr", Value = 803 * 1024 * 1024 },
            new DataItem { Category = "May", Value = 700 },
            new DataItem { Category = "Jun", Value = 650 },
            new DataItem { Category = "Jul", Value = 680 },
            new DataItem { Category = "Aug", Value = 590 },
        };

        private static async Task<IRenderedComponent<RadzenChart>> RenderAsync(TestContext ctx, object min, object max, double height)
        {
            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenLineSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Title, "Revenue")
                    .Add(x => x.Data, Revenue))
                .AddChildContent<RadzenValueAxis>(a => a
                    .Add(x => x.Min, min)
                    .Add(x => x.Max, max)));

            await chart.InvokeAsync(() => chart.Instance.Resize(800, height));

            return chart;
        }

        private static string[] ValueAxisLabels(IRenderedComponent<RadzenChart> chart)
        {
            return chart.FindAll("g.rz-value-axis .rz-tick-text").Select(t => t.TextContent.Trim()).ToArray();
        }

        [Theory]
        [InlineData(300)]
        [InlineData(400)]
        [InlineData(500)]
        [InlineData(700)]
        public async Task MinAndMax_AreBothLabelled(double height)
        {
            using var ctx = CreateChartContext();

            var chart = await RenderAsync(ctx, 512, 1073741824, height);

            var labels = ValueAxisLabels(chart);

            Assert.Equal("512", labels.First());
            Assert.Equal("1073741824", labels.Last());
        }

        [Theory]
        [InlineData(300)]
        [InlineData(500)]
        public async Task MinAndMax_SmallRange_AreBothLabelled(double height)
        {
            using var ctx = CreateChartContext();

            var chart = await RenderAsync(ctx, 0, 100, height);

            var labels = ValueAxisLabels(chart);

            Assert.Equal("0", labels.First());
            Assert.Equal("100", labels.Last());
        }
    }
}
