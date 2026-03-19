using Bunit;
using Xunit;
using static Radzen.Blazor.Tests.ChartTestHelper;

namespace Radzen.Blazor.Tests
{
    public class AreaSeriesTests
    {
        [Fact]
        public void AreaSeries_Renders_AreaClass()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenAreaSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Data, SampleData)));

            Assert.Contains("rz-area-series", chart.Markup);
        }

        [Fact]
        public void AreaSeries_CustomFillAndStroke()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenAreaSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Fill, "rgba(255,0,0,0.3)")
                    .Add(x => x.Stroke, "#FF0000")
                    .Add(x => x.Data, SampleData)));

            Assert.Contains("#FF0000", chart.Markup);
            Assert.Contains("rgba(255,0,0,0.3)", chart.Markup);
        }

        [Fact]
        public void AreaSeries_Smooth_SetsSplineInterpolation()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenAreaSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Smooth, true)
                    .Add(x => x.Data, SampleData)));

            var series = chart.FindComponent<RadzenAreaSeries<DataItem>>();
            Assert.Equal(Interpolation.Spline, series.Instance.Interpolation);
        }

        [Fact]
        public void AreaSeries_Hidden_DoesNotRender()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenAreaSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Visible, false)
                    .Add(x => x.Data, SampleData)));

            Assert.DoesNotContain("rz-area-series", chart.Markup);
        }
    }
}
