using Bunit;
using Xunit;
using static Radzen.Blazor.Tests.ChartTestHelper;

namespace Radzen.Blazor.Tests
{
    public class AnimateDataUpdatesTests
    {
        static void AssertTransitionMarkup(string markup, bool expected)
        {
            if (expected)
            {
                Assert.Contains("rz-path-transition", markup);
                Assert.Contains("d: path(", markup);
            }
            else
            {
                Assert.DoesNotContain("rz-path-transition", markup);
            }
        }

        static IRenderedComponent<RadzenChart> Render<TSeries>(TestContext ctx, bool animate) where TSeries : Radzen.Blazor.CartesianSeries<DataItem>
        {
            return ctx.RenderComponent<RadzenChart>(p => p
                .Add(x => x.AnimateDataUpdates, animate)
                .AddChildContent<TSeries>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Data, SampleData)));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async System.Threading.Tasks.Task LineSeries_EmitsTransitionMarkup(bool animate)
        {
            using var ctx = CreateChartContext();
            var chart = Render<RadzenLineSeries<DataItem>>(ctx, animate);
            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));
            AssertTransitionMarkup(chart.Markup, animate);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async System.Threading.Tasks.Task AreaSeries_EmitsTransitionMarkup(bool animate)
        {
            using var ctx = CreateChartContext();
            var chart = Render<RadzenAreaSeries<DataItem>>(ctx, animate);
            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));
            AssertTransitionMarkup(chart.Markup, animate);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async System.Threading.Tasks.Task StackedLineSeries_EmitsTransitionMarkup(bool animate)
        {
            using var ctx = CreateChartContext();
            var chart = Render<RadzenStackedLineSeries<DataItem>>(ctx, animate);
            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));
            AssertTransitionMarkup(chart.Markup, animate);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async System.Threading.Tasks.Task StackedAreaSeries_EmitsTransitionMarkup(bool animate)
        {
            using var ctx = CreateChartContext();
            var chart = Render<RadzenStackedAreaSeries<DataItem>>(ctx, animate);
            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));
            AssertTransitionMarkup(chart.Markup, animate);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async System.Threading.Tasks.Task FullStackedLineSeries_EmitsTransitionMarkup(bool animate)
        {
            using var ctx = CreateChartContext();
            var chart = Render<RadzenFullStackedLineSeries<DataItem>>(ctx, animate);
            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));
            AssertTransitionMarkup(chart.Markup, animate);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async System.Threading.Tasks.Task FullStackedAreaSeries_EmitsTransitionMarkup(bool animate)
        {
            using var ctx = CreateChartContext();
            var chart = Render<RadzenFullStackedAreaSeries<DataItem>>(ctx, animate);
            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));
            AssertTransitionMarkup(chart.Markup, animate);
        }
    }
}
