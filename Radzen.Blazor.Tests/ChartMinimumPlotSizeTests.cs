using System;
using System.Globalization;
using System.Linq;
using Bunit;
using Xunit;
using static Radzen.Blazor.Tests.ChartTestHelper;

namespace Radzen.Blazor.Tests
{
    public class ChartMinimumPlotSizeTests
    {
        private static DataItem[] VeryLongCategories(int count)
        {
            return Enumerable.Range(1, count)
                .Select(i => new DataItem { Category = $"Regional distribution warehouse and logistics center number {i:00} of the northern district", Value = 10 + i })
                .ToArray();
        }

        private static IRenderedComponent<RadzenChart> Render(TestContext ctx, double width, double height, string type, DataItem[] data,
            Action<ComponentParameterCollectionBuilder<RadzenCategoryAxis>> axis = null,
            Action<ComponentParameterCollectionBuilder<RadzenLegend>> legend = null,
            string title = "Value")
        {
            return ctx.RenderComponent<RadzenChart>(p =>
            {
                p.Add(x => x.Style, FormattableString.Invariant($"width: {width}px; height: {height}px"));

                if (type == "bar")
                {
                    p.AddChildContent<RadzenBarSeries<DataItem>>(s => s
                        .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                        .Add(x => x.ValueProperty, nameof(DataItem.Value))
                        .Add(x => x.Title, title)
                        .Add(x => x.Data, data));
                }
                else
                {
                    p.AddChildContent<RadzenColumnSeries<DataItem>>(s => s
                        .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                        .Add(x => x.ValueProperty, nameof(DataItem.Value))
                        .Add(x => x.Title, title)
                        .Add(x => x.Data, data));
                }

                p.AddChildContent<RadzenCategoryAxis>(axis ?? (_ => { }));

                if (legend != null)
                {
                    p.AddChildContent<RadzenLegend>(legend);
                }
            });
        }

        private static void AssertPlotIsAtLeastTheMinimum(IRenderedComponent<RadzenChart> chart, double width, double height)
        {
            var horizontal = chart.Instance.CategoryScale.Output;
            var vertical = chart.Instance.ValueScale.Output;

            Assert.True(horizontal.End - horizontal.Start >= Math.Min(80, width / 2) - 1e-9, $"plot width {horizontal.Start}..{horizontal.End}");
            Assert.True(vertical.Start - vertical.End >= Math.Min(80, height / 2) - 1e-9, $"plot height {vertical.End}..{vertical.Start}");
            Assert.True(horizontal.Start >= 0 && horizontal.End <= width, $"plot x {horizontal.Start}..{horizontal.End}");
            Assert.True(vertical.End >= 0 && vertical.Start <= height, $"plot y {vertical.End}..{vertical.Start}");
        }

        private static void AssertBottomAxisIncreases(IRenderedComponent<RadzenChart> chart)
        {
            var ticks = chart.FindAll("g.rz-category-axis .rz-tick-text")
                .Select(t => (Value: double.Parse(t.TextContent, CultureInfo.InvariantCulture), X: double.Parse(t.GetAttribute("x"), CultureInfo.InvariantCulture)))
                .ToList();

            Assert.True(ticks.Count > 1);
            Assert.Equal(ticks.OrderBy(t => t.Value).Select(t => t.X), ticks.Select(t => t.X).OrderBy(x => x));
        }

        [Theory]
        [InlineData(CategoryAxisLabelFit.None)]
        [InlineData(CategoryAxisLabelFit.Auto)]
        public void BarChartWithLabelsWiderThanTheChartKeepsTheMinimumPlot(CategoryAxisLabelFit fit)
        {
            using var ctx = CreateChartContext();

            var chart = Render(ctx, 390, 360, "bar", VeryLongCategories(12), a => a.Add(x => x.LabelFit, fit));

            AssertPlotIsAtLeastTheMinimum(chart, 390, 360);
            AssertBottomAxisIncreases(chart);
        }

        [Theory]
        [InlineData(CategoryAxisLabelFit.None)]
        [InlineData(CategoryAxisLabelFit.Auto)]
        public void ColumnChartWithLabelsWiderThanTheChartKeepsTheMinimumPlot(CategoryAxisLabelFit fit)
        {
            using var ctx = CreateChartContext();

            var chart = Render(ctx, 390, 360, "column", VeryLongCategories(12), a => a.Add(x => x.LabelFit, fit));

            AssertPlotIsAtLeastTheMinimum(chart, 390, 360);
        }

        [Fact]
        public void ColumnChartWithRotatedLabelsTallerThanTheChartKeepsTheMinimumPlot()
        {
            using var ctx = CreateChartContext();

            var chart = Render(ctx, 390, 360, "column", VeryLongCategories(12), a => a.Add(x => x.LabelRotation, -45).Add(x => x.LabelFit, CategoryAxisLabelFit.None));

            AssertPlotIsAtLeastTheMinimum(chart, 390, 360);
        }

        [Theory]
        [InlineData("bar", CategoryAxisLabelFit.None)]
        [InlineData("bar", CategoryAxisLabelFit.Auto)]
        [InlineData("column", CategoryAxisLabelFit.None)]
        [InlineData("column", CategoryAxisLabelFit.Auto)]
        public void TinyChartKeepsHalfItsSizeForThePlot(string type, CategoryAxisLabelFit fit)
        {
            using var ctx = CreateChartContext();

            var chart = Render(ctx, 120, 100, type, VeryLongCategories(6), a => a.Add(x => x.LabelFit, fit));

            AssertPlotIsAtLeastTheMinimum(chart, 120, 100);
        }

        [Theory]
        [InlineData(LegendPosition.Left)]
        [InlineData(LegendPosition.Right)]
        public void SideLegendAndLongLabelsKeepTheMinimumPlot(LegendPosition position)
        {
            using var ctx = CreateChartContext();

            var chart = Render(ctx, 390, 360, "bar", VeryLongCategories(12), a => a.Add(x => x.LabelFit, CategoryAxisLabelFit.None),
                l => l.Add(x => x.Position, position), "Revenue of every regional warehouse and logistics center in the northern district");

            AssertPlotIsAtLeastTheMinimum(chart, 390, 360);
            AssertBottomAxisIncreases(chart);
        }

        [Theory]
        [InlineData(LegendPosition.Top)]
        [InlineData(LegendPosition.Bottom)]
        public void TopOrBottomLegendAndRotatedLabelsKeepTheMinimumPlot(LegendPosition position)
        {
            using var ctx = CreateChartContext();

            var chart = Render(ctx, 390, 200, "column", VeryLongCategories(12), a => a.Add(x => x.LabelAutoRotation, -45).Add(x => x.LabelFit, CategoryAxisLabelFit.None),
                l => l.Add(x => x.Position, position));

            AssertPlotIsAtLeastTheMinimum(chart, 390, 200);
        }

        [Fact]
        public void MarginsThatFitAreUnchanged()
        {
            Assert.Equal((40d, 32d), RadzenChart.ClampMargins(40, 32, 390));
            Assert.Equal((230d, 80d), RadzenChart.ClampMargins(500, 80, 390));
            Assert.Equal((32d, 278d), RadzenChart.ClampMargins(32, 500, 390));
            Assert.Equal((155d, 155d), RadzenChart.ClampMargins(400, 300, 390));
            Assert.Equal((30d, 30d), RadzenChart.ClampMargins(100, 100, 120));
            Assert.Equal((100d, 100d), RadzenChart.ClampMargins(100, 100, 0));
        }
    }
}
