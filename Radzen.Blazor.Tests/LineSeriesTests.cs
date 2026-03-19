using Bunit;
using Xunit;
using static Radzen.Blazor.Tests.ChartTestHelper;

namespace Radzen.Blazor.Tests
{
    public class LineSeriesTests
    {
        [Fact]
        public void LineSeries_Renders_PathElement()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenLineSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Data, SampleData)));

            Assert.Contains("rz-line-series", chart.Markup);
            Assert.Contains("M ", chart.Markup); // SVG path data
        }

        [Fact]
        public void LineSeries_Renders_CustomStrokeColor()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenLineSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Stroke, "#FF0000")
                    .Add(x => x.Data, SampleData)));

            Assert.Contains("#FF0000", chart.Markup);
        }

        [Fact]
        public void LineSeries_Smooth_SetsInterpolationToSpline()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenLineSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Smooth, true)
                    .Add(x => x.Data, SampleData)));

            var series = chart.FindComponent<RadzenLineSeries<DataItem>>();
            Assert.Equal(Interpolation.Spline, series.Instance.Interpolation);
        }

        [Fact]
        public void LineSeries_MultipleSeries_RendersMultipleGroups()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenLineSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Title, "Series 1")
                    .Add(x => x.Data, SampleData))
                .AddChildContent<RadzenLineSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value2))
                    .Add(x => x.Title, "Series 2")
                    .Add(x => x.Data, SampleData)));

            Assert.Contains("rz-series-0", chart.Markup);
            Assert.Contains("rz-series-1", chart.Markup);
        }

        [Fact]
        public void LineSeries_Title_AppearsInLegend()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenLineSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Title, "Revenue")
                    .Add(x => x.Data, SampleData)));

            Assert.Contains("Revenue", chart.Markup);
        }

        [Fact]
        public void LineSeries_Hidden_DoesNotRenderPath()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenLineSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Visible, false)
                    .Add(x => x.Data, SampleData)));

            Assert.DoesNotContain("rz-line-series", chart.Markup);
        }

        [Fact]
        public void LineSeries_Renders_CategoryAxisLabels()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenLineSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Data, SampleData)));

            // Category axis should render tick labels for each category
            Assert.Contains(">A<", chart.Markup);
            Assert.Contains(">B<", chart.Markup);
            Assert.Contains(">C<", chart.Markup);
        }

        [Fact]
        public void LineSeries_StrokeWidth_AppliedToPath()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenLineSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.StrokeWidth, 5)
                    .Add(x => x.Data, SampleData)));

            Assert.Contains("stroke-width: 5", chart.Markup);
        }
    }
}
