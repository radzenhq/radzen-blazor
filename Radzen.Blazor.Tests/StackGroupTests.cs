using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Xunit;
using static Radzen.Blazor.Tests.ChartTestHelper;

namespace Radzen.Blazor.Tests
{
    public class StackGroupTests
    {
        static (double Start, double End) HorizontalSpan(IRenderedComponent<RadzenChart> chart, int seriesIndex) => Span(chart, "rz-column-series", seriesIndex, "x");

        static (double Start, double End) VerticalSpan(IRenderedComponent<RadzenChart> chart, int seriesIndex, string seriesClass = "rz-column-series") => Span(chart, seriesClass, seriesIndex, "y");

        static (double Start, double End) Span(IRenderedComponent<RadzenChart> chart, string seriesClass, int seriesIndex, string axis)
        {
            var d = chart.Find($"g.{seriesClass}.rz-series-{seriesIndex} path").GetAttribute("d")!;
            var values = Regex.Matches(d, @"[ML] (?<x>[-\d.]+) (?<y>[-\d.]+)")
                .Select(m => double.Parse(m.Groups[axis].Value, CultureInfo.InvariantCulture))
                .ToList();

            return (values.Min(), values.Max());
        }

        static void AssertSameSpan((double Start, double End) expected, (double Start, double End) actual)
        {
            Assert.Equal(expected.Start, actual.Start, 3);
            Assert.Equal(expected.End, actual.End, 3);
        }

        static void AssertDisjoint((double Start, double End) first, (double Start, double End) second, double gap)
        {
            Assert.True(first.End < second.Start || second.End < first.Start, $"{first} overlaps {second}");
            Assert.Equal(gap, Math.Max(second.Start - first.End, first.Start - second.End), 3);
        }

        static async Task<IRenderedComponent<RadzenChart>> Render(TestContext ctx, Action<ComponentParameterCollectionBuilder<RadzenChart>> series)
        {
            var chart = ctx.RenderComponent<RadzenChart>(series);

            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));

            return chart;
        }

        static void StackedColumn(ComponentParameterCollectionBuilder<RadzenChart> p, string valueProperty, string? stackGroup = null)
        {
            p.AddChildContent<RadzenStackedColumnSeries<DataItem>>(s => s
                .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                .Add(x => x.ValueProperty, valueProperty)
                .Add(x => x.StackGroup, stackGroup)
                .Add(x => x.Data, SampleData));
        }

        static void StackedBar(ComponentParameterCollectionBuilder<RadzenChart> p, string valueProperty, string? stackGroup = null)
        {
            p.AddChildContent<RadzenStackedBarSeries<DataItem>>(s => s
                .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                .Add(x => x.ValueProperty, valueProperty)
                .Add(x => x.StackGroup, stackGroup)
                .Add(x => x.Data, SampleData));
        }

        [Fact]
        public async Task ColumnAndStackedColumn_RenderSideBySide()
        {
            using var ctx = CreateChartContext();

            var chart = await Render(ctx, p =>
            {
                p.AddChildContent<RadzenColumnSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Data, SampleData));
                StackedColumn(p, nameof(DataItem.Value));
                StackedColumn(p, nameof(DataItem.Value2));
            });

            var column = HorizontalSpan(chart, 0);
            var stackBottom = HorizontalSpan(chart, 1);
            var stackTop = HorizontalSpan(chart, 2);

            AssertSameSpan(stackBottom, stackTop);
            AssertDisjoint(column, stackBottom, chart.Instance.ColumnOptions.Margin);
            Assert.Equal(column.End - column.Start, stackBottom.End - stackBottom.Start, 3);
        }

        [Fact]
        public async Task StackGroups_RenderAsSeparateColumns()
        {
            using var ctx = CreateChartContext();

            var chart = await Render(ctx, p =>
            {
                StackedColumn(p, nameof(DataItem.Value), "a");
                StackedColumn(p, nameof(DataItem.Value2), "a");
                StackedColumn(p, nameof(DataItem.Value2), "b");
            });

            var first = HorizontalSpan(chart, 0);
            var second = HorizontalSpan(chart, 1);
            var other = HorizontalSpan(chart, 2);

            AssertSameSpan(first, second);
            AssertDisjoint(first, other, chart.Instance.ColumnOptions.Margin);

            var firstY = VerticalSpan(chart, 0);
            var secondY = VerticalSpan(chart, 1);
            var otherY = VerticalSpan(chart, 2);

            Assert.Equal(firstY.Start, secondY.End, 3);
            Assert.Equal(firstY.End, otherY.End, 3);
        }

        [Fact]
        public async Task StackGroups_ValueAxis_FitsTheTallestGroup()
        {
            using var ctx = CreateChartContext();

            var single = await Render(ctx, p =>
            {
                StackedColumn(p, nameof(DataItem.Value));
                StackedColumn(p, nameof(DataItem.Value2));
                StackedColumn(p, nameof(DataItem.Value2));
            });

            var grouped = await Render(ctx, p =>
            {
                StackedColumn(p, nameof(DataItem.Value), "a");
                StackedColumn(p, nameof(DataItem.Value2), "a");
                StackedColumn(p, nameof(DataItem.Value2), "b");
            });

            Assert.True(grouped.Instance.ValueScale.Input.End < single.Instance.ValueScale.Input.End);
            Assert.True(grouped.Instance.ValueScale.Input.End >= 50);
        }

        [Fact]
        public async Task StackGroup_Change_MovesTheSeries()
        {
            using var ctx = CreateChartContext();

            static RenderFragment Series(string secondGroup) => builder =>
            {
                var seq = 0;

                foreach (var (valueProperty, group) in new[] { (nameof(DataItem.Value), "a"), (nameof(DataItem.Value2), secondGroup) })
                {
                    builder.OpenComponent<RadzenStackedColumnSeries<DataItem>>(seq++);
                    builder.AddAttribute(seq++, nameof(RadzenStackedColumnSeries<DataItem>.CategoryProperty), nameof(DataItem.Category));
                    builder.AddAttribute(seq++, nameof(RadzenStackedColumnSeries<DataItem>.ValueProperty), valueProperty);
                    builder.AddAttribute(seq++, nameof(RadzenStackedColumnSeries<DataItem>.StackGroup), group);
                    builder.AddAttribute(seq++, nameof(RadzenStackedColumnSeries<DataItem>.Data), SampleData);
                    builder.CloseComponent();
                }
            };

            var chart = await Render(ctx, p => p.Add(x => x.ChildContent, Series("a")));

            AssertSameSpan(HorizontalSpan(chart, 0), HorizontalSpan(chart, 1));

            chart.SetParametersAndRender(p => p.Add(x => x.ChildContent, Series("b")));

            AssertDisjoint(HorizontalSpan(chart, 0), HorizontalSpan(chart, 1), chart.Instance.ColumnOptions.Margin);
        }

        [Fact]
        public async Task BarAndStackedBar_RenderSideBySide()
        {
            using var ctx = CreateChartContext();

            var chart = await Render(ctx, p =>
            {
                p.AddChildContent<RadzenBarSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Data, SampleData));
                StackedBar(p, nameof(DataItem.Value));
                StackedBar(p, nameof(DataItem.Value2));
            });

            var bar = VerticalSpan(chart, 0, "rz-bar-series");
            var stackBottom = VerticalSpan(chart, 1, "rz-bar-series");
            var stackTop = VerticalSpan(chart, 2, "rz-bar-series");

            AssertSameSpan(stackBottom, stackTop);
            AssertDisjoint(bar, stackBottom, chart.Instance.BarOptions.Margin);
        }

        [Fact]
        public async Task StackedBarGroups_RenderAsSeparateBars()
        {
            using var ctx = CreateChartContext();

            var chart = await Render(ctx, p =>
            {
                StackedBar(p, nameof(DataItem.Value), "a");
                StackedBar(p, nameof(DataItem.Value2), "a");
                StackedBar(p, nameof(DataItem.Value2), "b");
            });

            AssertSameSpan(VerticalSpan(chart, 0, "rz-bar-series"), VerticalSpan(chart, 1, "rz-bar-series"));
            AssertDisjoint(VerticalSpan(chart, 0, "rz-bar-series"), VerticalSpan(chart, 2, "rz-bar-series"), chart.Instance.BarOptions.Margin);
        }

        [Fact]
        public async Task StackedColumn_ExplicitWidth_AppliesToEachGroup()
        {
            using var ctx = CreateChartContext();

            var chart = await Render(ctx, p =>
            {
                StackedColumn(p, nameof(DataItem.Value), "a");
                StackedColumn(p, nameof(DataItem.Value2), "b");
                p.AddChildContent<RadzenColumnOptions>(o => o.Add(x => x.Width, 20));
            });

            var first = HorizontalSpan(chart, 0);
            var second = HorizontalSpan(chart, 1);

            Assert.Equal(20, first.End - first.Start, 3);
            Assert.Equal(20, second.End - second.Start, 3);
            AssertDisjoint(first, second, chart.Instance.ColumnOptions.Margin);
        }
    }
}
