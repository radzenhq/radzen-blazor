using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Bunit;
using Xunit;
using static Radzen.Blazor.Tests.ChartTestHelper;

namespace Radzen.Blazor.Tests
{
    public class FunnelSeriesTests
    {
        private static readonly DataItem[] SkewedData = new[]
        {
            new DataItem { Category = "Visitors", Value = 179 },
            new DataItem { Category = "Leads", Value = 31 },
            new DataItem { Category = "Prospects", Value = 15 },
            new DataItem { Category = "Negotiations", Value = 9 },
        };

        private static List<(double Y, double Width)> ParseEdgeWidths(string markup)
        {
            var edges = new List<(double, double)>();

            foreach (Match match in Regex.Matches(markup, "--rz-segment-order:.*?d=\"M ([^\"]+)\"", RegexOptions.Singleline))
            {
                var numbers = Regex.Matches(match.Groups[1].Value, "-?[0-9]+(\\.[0-9]+)?")
                    .Select(m => double.Parse(m.Value, CultureInfo.InvariantCulture))
                    .ToArray();

                if (numbers.Length != 8)
                {
                    continue;
                }

                var topWidth = System.Math.Abs(numbers[2] - numbers[0]);
                var bottomWidth = System.Math.Abs(numbers[4] - numbers[6]);

                edges.Add((numbers[1], topWidth));
                edges.Add((numbers[5], bottomWidth));
            }

            return edges.OrderBy(e => e.Item1).ToList();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void FunnelSeries_SkewedValues_OutlineDoesNotFlareBack(bool inverted)
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenFunnelSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Inverted, inverted)
                    .Add(x => x.Data, SkewedData)));

            var edges = ParseEdgeWidths(chart.Markup);

            Assert.NotEmpty(edges);

            var widths = edges.Select(e => e.Width).ToList();
            const double tolerance = 0.01;

            var nonDecreasing = true;
            var nonIncreasing = true;

            for (var i = 1; i < widths.Count; i++)
            {
                if (widths[i] < widths[i - 1] - tolerance)
                {
                    nonDecreasing = false;
                }

                if (widths[i] > widths[i - 1] + tolerance)
                {
                    nonIncreasing = false;
                }
            }

            Assert.True(nonDecreasing || nonIncreasing,
                $"Funnel outline reverses direction (flares back out): [{string.Join(", ", widths)}].");
        }

        [Fact]
        public void FunnelSeries_Renders_FunnelClass()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenFunnelSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Data, SampleData)));

            Assert.Contains("rz-funnel-series", chart.Markup);
        }

        [Fact]
        public void FunnelSeries_Renders_SegmentPerItem()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenFunnelSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Data, SampleData)));

            Assert.Contains("rz-series-item-0", chart.Markup);
            Assert.Contains("rz-series-item-1", chart.Markup);
            Assert.Contains("rz-series-item-2", chart.Markup);
        }

        [Fact]
        public void FunnelSeries_ShowLabels_RendersCategoryNames()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenFunnelSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Data, SampleData)));

            Assert.Contains("A", chart.Markup);
            Assert.Contains("B", chart.Markup);
            Assert.Contains("C", chart.Markup);
        }

        [Fact]
        public void FunnelSeries_CustomFills_Applied()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenFunnelSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Fills, new[] { "#FF0000", "#00FF00", "#0000FF" })
                    .Add(x => x.Data, SampleData)));

            Assert.Contains("#FF0000", chart.Markup);
            Assert.Contains("#00FF00", chart.Markup);
            Assert.Contains("#0000FF", chart.Markup);
        }

        [Fact]
        public void FunnelSeries_Hidden_DoesNotRender()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenFunnelSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Visible, false)
                    .Add(x => x.Data, SampleData)));

            Assert.DoesNotContain("rz-funnel-series", chart.Markup);
        }
    }
}
