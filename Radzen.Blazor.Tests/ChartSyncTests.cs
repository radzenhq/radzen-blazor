using System.Linq;
using Bunit;
using Xunit;
using static Radzen.Blazor.Tests.ChartTestHelper;

namespace Radzen.Blazor.Tests
{
    public class ChartSyncTests
    {
        static IRenderedComponent<RadzenChart> RenderSyncedChart(TestContext ctx, string group)
        {
            return ctx.RenderComponent<RadzenChart>(p => p
                .Add(x => x.SyncGroup, group)
                .AddChildContent<RadzenLineSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Data, SampleData)));
        }

        [Fact]
        public async System.Threading.Tasks.Task SyncedHover_OnlyBroadcastsInsidePlotArea()
        {
            using var ctx = CreateChartContext();
            ctx.JSInterop.SetupVoid("Radzen.openChartTooltip", _ => true);
            ctx.RenderComponent<RadzenChartTooltip>();

            // Unique group per test - the sync registry is static.
            var group = $"test-{System.Guid.NewGuid():N}";
            var source = RenderSyncedChart(ctx, group);
            var receiver = RenderSyncedChart(ctx, group);

            await source.InvokeAsync(() => source.Instance.Resize(400, 300));
            await receiver.InvokeAsync(() => receiver.Instance.Resize(400, 300));

            // Cursor over the source's top margin (inside the element, outside the plot):
            // the receiver must show nothing.
            await source.InvokeAsync(() => source.Instance.MouseMove(200, 2));
            Assert.DoesNotContain("rz-chart-category-tooltip", receiver.Markup);
            Assert.DoesNotContain("rz-active-point", receiver.Markup);

            // Cursor inside the source's plot: the receiver shows the synced tooltip box and dot.
            await source.InvokeAsync(() => source.Instance.MouseMove(200, 150));
            Assert.Contains("rz-chart-category-tooltip", receiver.Markup);
            Assert.Contains("rz-active-point", receiver.Markup);

            // Back to the margin: the receiver clears again.
            await source.InvokeAsync(() => source.Instance.MouseMove(200, 2));
            Assert.DoesNotContain("rz-chart-category-tooltip", receiver.Markup);
            Assert.DoesNotContain("rz-active-point", receiver.Markup);
        }
    }
}
