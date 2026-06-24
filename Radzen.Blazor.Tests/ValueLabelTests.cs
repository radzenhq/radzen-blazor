using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Bunit;
using Xunit;
using static Radzen.Blazor.Tests.ChartTestHelper;

namespace Radzen.Blazor.Tests
{
    public class ValueLabelTests
    {
        static IRenderedComponent<RadzenChart> RenderChartWithValueLabels(TestContext ctx, double lastValue1, double lastValue2)
        {
            var data1 = new[]
            {
                new DataItem { Category = "A", Value = 10 },
                new DataItem { Category = "B", Value = lastValue1 }
            };
            var data2 = new[]
            {
                new DataItem { Category = "A", Value = 30 },
                new DataItem { Category = "B", Value = lastValue2 }
            };

            return ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenLineSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Data, data1)
                    .AddChildContent<RadzenSeriesValueLabel>())
                .AddChildContent<RadzenLineSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Data, data2)
                    .AddChildContent<RadzenSeriesValueLabel>()));
        }

        static double[] LabelCenters(string markup)
        {
            // The pill rect y is the label center minus half the box height (18 / 2 = 9).
            return Regex.Matches(markup, "rz-series-value-label[\\s\\S]*?<rect[^>]*\\sy=\"([-\\d.]+)\"")
                .Select(m => double.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture) + 9)
                .ToArray();
        }

        [Fact]
        public async System.Threading.Tasks.Task ValueLabels_Overlapping_AreNudgedApart()
        {
            using var ctx = CreateChartContext();

            // Identical last values - without nudging both pills would render at the same Y.
            var chart = RenderChartWithValueLabels(ctx, 20, 20);

            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));

            var centers = LabelCenters(chart.Markup);

            Assert.Equal(2, centers.Length);
            // Reserved slot height is boxHeight (18) + 4px gap.
            Assert.True(Math.Abs(centers[0] - centers[1]) >= 18, $"Labels overlap: centers at {centers[0]} and {centers[1]}");
        }

        [Fact]
        public async System.Threading.Tasks.Task ValueLabels_RenderAboveAllSeries()
        {
            using var ctx = CreateChartContext();

            var chart = RenderChartWithValueLabels(ctx, 15, 25);

            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));

            // The label of the FIRST series must appear after the LAST series group in document
            // order (SVG z-order), so no series line can draw over a pill.
            var lastSeries = chart.Markup.LastIndexOf("rz-line-series", System.StringComparison.Ordinal);
            var firstLabel = chart.Markup.IndexOf("rz-series-value-label", System.StringComparison.Ordinal);

            Assert.True(firstLabel > lastSeries, $"value label at {firstLabel} should render after the last series at {lastSeries}");
        }

        [Fact]
        public async System.Threading.Tasks.Task ValueLabels_FarApart_AreNotNudged()
        {
            using var ctx = CreateChartContext();

            var chart = RenderChartWithValueLabels(ctx, 10, 40);

            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));

            var centers = LabelCenters(chart.Markup);

            Assert.Equal(2, centers.Length);
            // Far-apart values keep their natural positions - way more than one slot apart.
            Assert.True(Math.Abs(centers[0] - centers[1]) > 50, $"Labels unexpectedly close: {centers[0]} and {centers[1]}");
        }
    }
}
