using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Bunit;
using Xunit;
using static Radzen.Blazor.Tests.ChartTestHelper;

namespace Radzen.Blazor.Tests
{
    public class ValueAxisRangeTests
    {
        private const string LeftValueAxis = "g.rz-value-axis:not(.rz-value-axis-right):not(.rz-value-axis-top)";
        private const string RightValueAxis = "g.rz-value-axis-right";
        private const string TopValueAxis = "g.rz-value-axis-top";
        private const string BottomAxis = "g.rz-category-axis";

        private static DataItem[] Values(params double[] values)
        {
            return values.Select((value, index) => new DataItem { Category = ((char)('A' + index)).ToString(), Value = value, Value2 = value * 2 }).ToArray();
        }

        private static DataItem[] Values(string sign)
        {
            return sign switch
            {
                "positive" => Values(10, 20, 15),
                "negative" => Values(-10, -20, -15),
                "mixed" => Values(-10, 20, 15),
                _ => throw new ArgumentOutOfRangeException(nameof(sign))
            };
        }

        private static async Task<IRenderedComponent<RadzenChart>> RenderAsync(TestContext ctx, Action<ComponentParameterCollectionBuilder<RadzenChart>> content)
        {
            var chart = ctx.RenderComponent<RadzenChart>(content);

            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));

            return chart;
        }

        private static string Labels(IRenderedComponent<RadzenChart> chart, string axis)
        {
            return string.Join("|", chart.FindAll($"{axis} .rz-tick-text").Select(t => t.TextContent.Trim()));
        }

        private static List<(double X, double Y)> Points(IRenderedFragment chart, string series, int index)
        {
            var d = chart.FindAll($"{series} path")[index].GetAttribute("d") ?? string.Empty;

            return Regex.Matches(d, @"[ML]\s+(\S+)\s+(\S+)")
                .Select(m => (double.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture), double.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture)))
                .ToList();
        }

        private static (double Top, double Bottom) ColumnExtent(IRenderedComponent<RadzenChart> chart, int index)
        {
            var points = Points(chart, "g.rz-column-series", index);

            return (Math.Round(points.Min(p => p.Y), 3), Math.Round(points.Max(p => p.Y), 3));
        }

        private static (double Left, double Right) BarExtent(IRenderedComponent<RadzenChart> chart, int index)
        {
            var points = Points(chart, "g.rz-bar-series", index);

            return (Math.Round(points.Min(p => p.X), 3), Math.Round(points.Max(p => p.X), 3));
        }

        private static double FirstLinePointY(IRenderedComponent<RadzenChart> chart)
        {
            return Math.Round(Points(chart, "g.rz-line-series", 0)[0].Y, 3);
        }

        private static Action<ComponentParameterCollectionBuilder<RadzenColumnSeries<DataItem>>> Column(DataItem[] data, string valueAxisName = null)
        {
            return s =>
            {
                s.Add(x => x.CategoryProperty, nameof(DataItem.Category))
                 .Add(x => x.ValueProperty, nameof(DataItem.Value))
                 .Add(x => x.Data, data);

                if (valueAxisName != null)
                {
                    s.Add(x => x.ValueAxisName, valueAxisName);
                }
            };
        }

        private static Action<ComponentParameterCollectionBuilder<RadzenBarSeries<DataItem>>> Bar(DataItem[] data, string valueAxisName = null)
        {
            return s =>
            {
                s.Add(x => x.CategoryProperty, nameof(DataItem.Category))
                 .Add(x => x.ValueProperty, nameof(DataItem.Value))
                 .Add(x => x.Data, data);

                if (valueAxisName != null)
                {
                    s.Add(x => x.ValueAxisName, valueAxisName);
                }
            };
        }

        private static Action<ComponentParameterCollectionBuilder<RadzenLineSeries<DataItem>>> Line(DataItem[] data, string valueProperty = nameof(DataItem.Value), string valueAxisName = null)
        {
            return s =>
            {
                s.Add(x => x.CategoryProperty, nameof(DataItem.Category))
                 .Add(x => x.ValueProperty, valueProperty)
                 .Add(x => x.Data, data);

                if (valueAxisName != null)
                {
                    s.Add(x => x.ValueAxisName, valueAxisName);
                }
            };
        }

        private static Action<ComponentParameterCollectionBuilder<RadzenValueAxis>> Axis(ValueAxisRange range, string name = null)
        {
            return a =>
            {
                a.Add(x => x.Range, range);

                if (name != null)
                {
                    a.Add(x => x.Name, name);
                }
            };
        }

        [Fact]
        public void Range_DefaultsToAuto()
        {
            Assert.Equal(ValueAxisRange.Auto, new RadzenValueAxis().Range);
        }

        [Theory]
        [InlineData("positive", ValueAxisRange.Auto, "0|5|10|15|20", 118, 236)]
        [InlineData("positive", ValueAxisRange.Data, "10|15|20", 236, 236)]
        [InlineData("positive", ValueAxisRange.IncludeZero, "0|5|10|15|20", 118, 236)]
        [InlineData("negative", ValueAxisRange.Auto, "-20|-15|-10|-5|0", 0, 118)]
        [InlineData("negative", ValueAxisRange.Data, "-20|-15|-10", -236, 0)]
        [InlineData("negative", ValueAxisRange.IncludeZero, "-20|-15|-10|-5|0", 0, 118)]
        [InlineData("mixed", ValueAxisRange.Auto, "-10|0|10|20", 157.333, 236)]
        [InlineData("mixed", ValueAxisRange.Data, "-10|0|10|20", 157.333, 236)]
        [InlineData("mixed", ValueAxisRange.IncludeZero, "-10|0|10|20", 157.333, 236)]
        public async Task Column_RangeDeterminesTheAxisAndTheColumnExtent(string sign, ValueAxisRange range, string labels, double top, double bottom)
        {
            using var ctx = CreateChartContext();

            var chart = await RenderAsync(ctx, p => p
                .AddChildContent<RadzenColumnSeries<DataItem>>(Column(Values(sign)))
                .AddChildContent<RadzenValueAxis>(Axis(range)));

            Assert.Equal(labels, Labels(chart, LeftValueAxis));
            Assert.Equal((top, bottom), ColumnExtent(chart, 0));
        }

        [Theory]
        [InlineData("positive", ValueAxisRange.Auto, "0|5|10|15|20", 0, 144.75)]
        [InlineData("positive", ValueAxisRange.Data, "10|15|20", 0, 0)]
        [InlineData("positive", ValueAxisRange.IncludeZero, "0|5|10|15|20", 0, 144.75)]
        [InlineData("negative", ValueAxisRange.Auto, "-20|-15|-10|-5|0", 144.75, 289.5)]
        [InlineData("negative", ValueAxisRange.Data, "-20|-15|-10", 289.5, 579)]
        [InlineData("negative", ValueAxisRange.IncludeZero, "-20|-15|-10|-5|0", 144.75, 289.5)]
        [InlineData("mixed", ValueAxisRange.Auto, "-10|0|10|20", 0, 96.5)]
        [InlineData("mixed", ValueAxisRange.Data, "-10|0|10|20", 0, 96.5)]
        [InlineData("mixed", ValueAxisRange.IncludeZero, "-10|0|10|20", 0, 96.5)]
        public async Task Bar_RangeDeterminesTheAxisAndTheBarExtent(string sign, ValueAxisRange range, string labels, double left, double right)
        {
            using var ctx = CreateChartContext();

            var chart = await RenderAsync(ctx, p => p
                .AddChildContent<RadzenBarSeries<DataItem>>(Bar(Values(sign)))
                .AddChildContent<RadzenValueAxis>(Axis(range)));

            Assert.Equal(labels, Labels(chart, BottomAxis));
            Assert.Equal((left, right), BarExtent(chart, 0));
        }

        [Theory]
        [InlineData("positive", ValueAxisRange.Auto, "10|15|20", 236)]
        [InlineData("positive", ValueAxisRange.Data, "10|15|20", 236)]
        [InlineData("positive", ValueAxisRange.IncludeZero, "0|5|10|15|20", 118)]
        [InlineData("negative", ValueAxisRange.Auto, "-20|-15|-10", 0)]
        [InlineData("negative", ValueAxisRange.Data, "-20|-15|-10", 0)]
        [InlineData("negative", ValueAxisRange.IncludeZero, "-20|-15|-10|-5|0", 118)]
        [InlineData("mixed", ValueAxisRange.Auto, "-10|0|10|20", 236)]
        [InlineData("mixed", ValueAxisRange.Data, "-10|0|10|20", 236)]
        [InlineData("mixed", ValueAxisRange.IncludeZero, "-10|0|10|20", 236)]
        public async Task Line_RangeDeterminesTheAxisAndThePointPosition(string sign, ValueAxisRange range, string labels, double firstPointY)
        {
            using var ctx = CreateChartContext();

            var chart = await RenderAsync(ctx, p => p
                .AddChildContent<RadzenLineSeries<DataItem>>(Line(Values(sign)))
                .AddChildContent<RadzenValueAxis>(Axis(range)));

            Assert.Equal(labels, Labels(chart, LeftValueAxis));
            Assert.Equal(firstPointY, FirstLinePointY(chart));
        }

        [Theory]
        [InlineData("positive", ValueAxisRange.Auto)]
        [InlineData("positive", ValueAxisRange.Data)]
        [InlineData("positive", ValueAxisRange.IncludeZero)]
        [InlineData("negative", ValueAxisRange.Auto)]
        [InlineData("negative", ValueAxisRange.Data)]
        [InlineData("negative", ValueAxisRange.IncludeZero)]
        public async Task StackedColumn_IncludesZeroInEveryRange(string sign, ValueAxisRange range)
        {
            using var ctx = CreateChartContext();

            var chart = await RenderAsync(ctx, p => p
                .AddChildContent<RadzenStackedColumnSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Data, Values(sign)))
                .AddChildContent<RadzenValueAxis>(Axis(range)));

            Assert.Equal(sign == "positive" ? "0|5|10|15|20" : "-20|-15|-10|-5|0", Labels(chart, LeftValueAxis));
            Assert.Equal(sign == "positive" ? (118d, 236d) : (0d, 118d), ColumnExtent(chart, 0));
        }

        [Theory]
        [InlineData("positive", ValueAxisRange.Auto)]
        [InlineData("positive", ValueAxisRange.Data)]
        [InlineData("positive", ValueAxisRange.IncludeZero)]
        [InlineData("negative", ValueAxisRange.Auto)]
        [InlineData("negative", ValueAxisRange.Data)]
        [InlineData("negative", ValueAxisRange.IncludeZero)]
        public async Task StackedBar_IncludesZeroInEveryRange(string sign, ValueAxisRange range)
        {
            using var ctx = CreateChartContext();

            var chart = await RenderAsync(ctx, p => p
                .AddChildContent<RadzenStackedBarSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Data, Values(sign)))
                .AddChildContent<RadzenValueAxis>(Axis(range)));

            Assert.Equal(sign == "positive" ? "0|5|10|15|20" : "-20|-15|-10|-5|0", Labels(chart, BottomAxis));
            Assert.Equal(sign == "positive" ? (0d, 144.75d) : (144.75d, 289.5d), BarExtent(chart, 0));
        }

        [Theory]
        [InlineData("positive", ValueAxisRange.Auto)]
        [InlineData("positive", ValueAxisRange.Data)]
        [InlineData("positive", ValueAxisRange.IncludeZero)]
        [InlineData("negative", ValueAxisRange.Auto)]
        [InlineData("negative", ValueAxisRange.Data)]
        [InlineData("negative", ValueAxisRange.IncludeZero)]
        public async Task StackedArea_IncludesZeroInEveryRange(string sign, ValueAxisRange range)
        {
            using var ctx = CreateChartContext();

            var chart = await RenderAsync(ctx, p => p
                .AddChildContent<RadzenStackedAreaSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Data, Values(sign)))
                .AddChildContent<RadzenValueAxis>(Axis(range)));

            Assert.Equal(sign == "positive" ? "0|5|10|15|20" : "-20|-15|-10|-5|0", Labels(chart, LeftValueAxis));
        }

        [Theory]
        [InlineData("positive", ValueAxisRange.Auto, "10|15|20")]
        [InlineData("positive", ValueAxisRange.Data, "10|15|20")]
        [InlineData("positive", ValueAxisRange.IncludeZero, "0|5|10|15|20")]
        [InlineData("negative", ValueAxisRange.Auto, "-20|-15|-10")]
        [InlineData("negative", ValueAxisRange.Data, "-20|-15|-10")]
        [InlineData("negative", ValueAxisRange.IncludeZero, "-20|-15|-10|-5|0")]
        public async Task StackedLine_FitsItsTotalsUnlessTheRangeIsIncludeZero(string sign, ValueAxisRange range, string labels)
        {
            using var ctx = CreateChartContext();

            var chart = await RenderAsync(ctx, p => p
                .AddChildContent<RadzenStackedLineSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Data, Values(sign)))
                .AddChildContent<RadzenValueAxis>(Axis(range)));

            Assert.Equal(labels, Labels(chart, LeftValueAxis));
        }

        [Theory]
        [InlineData("positive", ValueAxisRange.Auto, "0|5|10|15|20", 236, 118)]
        [InlineData("positive", ValueAxisRange.Data, "10|15|20", 236, 236)]
        [InlineData("positive", ValueAxisRange.IncludeZero, "0|5|10|15|20", 236, 118)]
        [InlineData("negative", ValueAxisRange.Auto, "-20|-15|-10|-5|0", 0, 118)]
        [InlineData("negative", ValueAxisRange.Data, "-20|-15|-10", -236, 0)]
        [InlineData("negative", ValueAxisRange.IncludeZero, "-20|-15|-10|-5|0", 0, 118)]
        [InlineData("mixed", ValueAxisRange.Auto, "-10|0|10|20", 157.333, 236)]
        [InlineData("mixed", ValueAxisRange.Data, "-10|0|10|20", 157.333, 236)]
        [InlineData("mixed", ValueAxisRange.IncludeZero, "-10|0|10|20", 157.333, 236)]
        public async Task Area_RangeDeterminesTheAxisAndTheFill(string sign, ValueAxisRange range, string labels, double baselineY, double firstPointY)
        {
            using var ctx = CreateChartContext();

            var chart = await RenderAsync(ctx, p => p
                .AddChildContent<RadzenAreaSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Data, Values(sign)))
                .AddChildContent<RadzenValueAxis>(Axis(range)));

            var fill = Regex.Matches(chart.Find("g.rz-area-series path[stroke='none']").GetAttribute("d"), @"-?\d+(\.\d+)?(E-?\d+)?")
                .Select(m => Math.Round(double.Parse(m.Value, CultureInfo.InvariantCulture), 3))
                .ToList();

            Assert.Equal(labels, Labels(chart, LeftValueAxis));
            Assert.Equal(baselineY, fill[1]);
            Assert.Equal(firstPointY, fill[3]);
        }

        [Fact]
        public async Task RangeArea_FitsItsValuesByDefault()
        {
            using var ctx = CreateChartContext();

            var data = new[]
            {
                new DataItem { Category = "A", Min = 10, Max = 20 },
                new DataItem { Category = "B", Min = 12, Max = 18 },
                new DataItem { Category = "C", Min = 11, Max = 19 },
            };

            var chart = await RenderAsync(ctx, p => p
                .AddChildContent<RadzenRangeAreaSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.MinProperty, nameof(DataItem.Min))
                    .Add(x => x.MaxProperty, nameof(DataItem.Max))
                    .Add(x => x.Data, data)));

            Assert.Equal("10|15|20", Labels(chart, LeftValueAxis));
        }

        [Theory]
        [InlineData("positive", ValueAxisRange.Auto)]
        [InlineData("positive", ValueAxisRange.Data)]
        [InlineData("positive", ValueAxisRange.IncludeZero)]
        [InlineData("negative", ValueAxisRange.Auto)]
        [InlineData("negative", ValueAxisRange.Data)]
        [InlineData("negative", ValueAxisRange.IncludeZero)]
        public async Task FullStackedColumn_SpansZeroToHundredInEveryRange(string sign, ValueAxisRange range)
        {
            using var ctx = CreateChartContext();

            var chart = await RenderAsync(ctx, p => p
                .AddChildContent<RadzenFullStackedColumnSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Data, Values(sign)))
                .AddChildContent<RadzenValueAxis>(Axis(range)));

            Assert.Equal("0|50|100", Labels(chart, LeftValueAxis));
        }

        [Theory]
        [InlineData("positive", ValueAxisRange.Auto)]
        [InlineData("positive", ValueAxisRange.Data)]
        [InlineData("positive", ValueAxisRange.IncludeZero)]
        [InlineData("negative", ValueAxisRange.Auto)]
        [InlineData("negative", ValueAxisRange.Data)]
        [InlineData("negative", ValueAxisRange.IncludeZero)]
        public async Task Waterfall_IncludesZeroInEveryRange(string sign, ValueAxisRange range)
        {
            using var ctx = CreateChartContext();

            var chart = await RenderAsync(ctx, p => p
                .AddChildContent<RadzenWaterfallSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Data, Values(sign)))
                .AddChildContent<RadzenValueAxis>(Axis(range)));

            Assert.Equal(sign == "positive" ? "0|20|40|60" : "-60|-40|-20|0", Labels(chart, LeftValueAxis));
        }

        [Theory]
        [InlineData("positive", ValueAxisRange.Auto)]
        [InlineData("positive", ValueAxisRange.Data)]
        [InlineData("positive", ValueAxisRange.IncludeZero)]
        [InlineData("negative", ValueAxisRange.Auto)]
        [InlineData("negative", ValueAxisRange.Data)]
        [InlineData("negative", ValueAxisRange.IncludeZero)]
        public async Task HorizontalWaterfall_IncludesZeroInEveryRange(string sign, ValueAxisRange range)
        {
            using var ctx = CreateChartContext();

            var chart = await RenderAsync(ctx, p => p
                .AddChildContent<RadzenHorizontalWaterfallSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Data, Values(sign)))
                .AddChildContent<RadzenValueAxis>(Axis(range)));

            Assert.Equal(sign == "positive" ? "0|20|40|60" : "-60|-40|-20|0", Labels(chart, BottomAxis));
        }

        [Theory]
        [InlineData(ValueAxisRange.Auto, "0|10|20|30", 157.333, 236)]
        [InlineData(ValueAxisRange.Data, "10|15|20|25", 236, 236)]
        [InlineData(ValueAxisRange.IncludeZero, "0|10|20|30", 157.333, 236)]
        public async Task ColumnAndLineOnOneAxis_FollowTheColumn(ValueAxisRange range, string labels, double top, double bottom)
        {
            using var ctx = CreateChartContext();

            var chart = await RenderAsync(ctx, p => p
                .AddChildContent<RadzenColumnSeries<DataItem>>(Column(Values(10, 20, 15)))
                .AddChildContent<RadzenLineSeries<DataItem>>(Line(Values(15, 25, 20)))
                .AddChildContent<RadzenValueAxis>(Axis(range)));

            Assert.Equal(labels, Labels(chart, LeftValueAxis));
            Assert.Equal((top, bottom), ColumnExtent(chart, 0));
        }

        [Fact]
        public async Task ColumnWithEqualValuesAndWiderLineOnOneAxis_IncludeZeroWhateverTheSeriesOrder()
        {
            using var ctx = CreateChartContext();

            var chart = await RenderAsync(ctx, p => p
                .AddChildContent<RadzenColumnSeries<DataItem>>(Column(Values(10, 10, 10)))
                .AddChildContent<RadzenLineSeries<DataItem>>(Line(Values(15, 25, 20))));

            Assert.Equal("0|10|20|30", Labels(chart, LeftValueAxis));
            Assert.Equal((157.333, 236d), ColumnExtent(chart, 0));
        }

        [Theory]
        [InlineData(ValueAxisRange.Auto, "20|25|30|35|40")]
        [InlineData(ValueAxisRange.Data, "20|25|30|35|40")]
        [InlineData(ValueAxisRange.IncludeZero, "0|10|20|30|40")]
        public async Task ColumnOnPrimaryAxisAndLineOnNamedAxis_EachAxisDecides(ValueAxisRange namedRange, string namedLabels)
        {
            using var ctx = CreateChartContext();

            var chart = await RenderAsync(ctx, p => p
                .AddChildContent<RadzenColumnSeries<DataItem>>(Column(Values(10, 20, 15)))
                .AddChildContent<RadzenLineSeries<DataItem>>(Line(Values(10, 20, 15), nameof(DataItem.Value2), "second"))
                .AddChildContent<RadzenValueAxis>(Axis(namedRange, "second")));

            Assert.Equal("0|5|10|15|20", Labels(chart, LeftValueAxis));
            Assert.Equal(namedLabels, Labels(chart, RightValueAxis));
            Assert.Equal((118d, 236d), ColumnExtent(chart, 0));
        }

        [Fact]
        public async Task ColumnOnPrimaryAxisWithDataRangeAndColumnOnNamedAxis_EachAxisDecides()
        {
            using var ctx = CreateChartContext();

            var chart = await RenderAsync(ctx, p => p
                .AddChildContent<RadzenColumnSeries<DataItem>>(Column(Values(10, 20, 15)))
                .AddChildContent<RadzenColumnSeries<DataItem>>(Column(Values(10, 20, 15), "second"))
                .AddChildContent<RadzenValueAxis>(Axis(ValueAxisRange.Data))
                .AddChildContent<RadzenValueAxis>(Axis(ValueAxisRange.Auto, "second")));

            Assert.Equal("10|15|20", Labels(chart, LeftValueAxis));
            Assert.Equal("0|5|10|15|20", Labels(chart, RightValueAxis));
        }

        [Fact]
        public async Task BarOnPrimaryAxisAndBarOnNamedAxisWithDataRange_EachValueAxisDecides()
        {
            using var ctx = CreateChartContext();

            var chart = await RenderAsync(ctx, p => p
                .AddChildContent<RadzenBarSeries<DataItem>>(Bar(Values(10, 20, 15)))
                .AddChildContent<RadzenBarSeries<DataItem>>(Bar(Values(10, 20, 15), "second"))
                .AddChildContent<RadzenValueAxis>(Axis(ValueAxisRange.Data, "second")));

            Assert.Equal("0|5|10|15|20", Labels(chart, BottomAxis));
            Assert.Equal("10|15|20", Labels(chart, TopValueAxis));
        }

        [Fact]
        public async Task Column_MinWinsOverRange()
        {
            using var ctx = CreateChartContext();

            var chart = await RenderAsync(ctx, p => p
                .AddChildContent<RadzenColumnSeries<DataItem>>(Column(Values(10, 20, 15)))
                .AddChildContent<RadzenValueAxis>(a => a
                    .Add(x => x.Min, 5)
                    .Add(x => x.Range, ValueAxisRange.IncludeZero)));

            Assert.Equal("5|10|15|20", Labels(chart, LeftValueAxis));
            Assert.Equal((157.333, 236d), ColumnExtent(chart, 0));
        }

        [Fact]
        public async Task Column_MaxWinsOverRange()
        {
            using var ctx = CreateChartContext();

            var chart = await RenderAsync(ctx, p => p
                .AddChildContent<RadzenColumnSeries<DataItem>>(Column(Values(-10, -20, -15)))
                .AddChildContent<RadzenValueAxis>(a => a
                    .Add(x => x.Max, -5)
                    .Add(x => x.Range, ValueAxisRange.IncludeZero)));

            Assert.Equal("-20|-15|-10|-5", Labels(chart, LeftValueAxis));
        }

        [Fact]
        public async Task Column_LogarithmicAxis_IgnoresRange()
        {
            using var ctx = CreateChartContext();

            var chart = await RenderAsync(ctx, p => p
                .AddChildContent<RadzenColumnSeries<DataItem>>(Column(Values(10, 100, 1000)))
                .AddChildContent<RadzenValueAxis>(a => a
                    .Add(x => x.Logarithmic, true)
                    .Add(x => x.Range, ValueAxisRange.IncludeZero)));

            Assert.Equal("10|100|1000", Labels(chart, LeftValueAxis));
            Assert.Equal((236d, 236d), ColumnExtent(chart, 0));
            Assert.Equal((118d, 236d), ColumnExtent(chart, 1));
        }

        [Theory]
        [InlineData(ValueAxisRange.Auto)]
        [InlineData(ValueAxisRange.Data)]
        [InlineData(ValueAxisRange.IncludeZero)]
        public async Task Column_EmptyData_RendersTheDefaultAxis(ValueAxisRange range)
        {
            using var ctx = CreateChartContext();

            var chart = await RenderAsync(ctx, p => p
                .AddChildContent<RadzenColumnSeries<DataItem>>(Column(Values()))
                .AddChildContent<RadzenValueAxis>(Axis(range)));

            Assert.Equal("0|1|2", Labels(chart, LeftValueAxis));
        }

        [Theory]
        [InlineData(ValueAxisRange.Auto, 10, "0|5|10|15", 78.667, 236)]
        [InlineData(ValueAxisRange.Data, 10, "0|5|10|15", 78.667, 236)]
        [InlineData(ValueAxisRange.IncludeZero, 10, "0|5|10|15", 78.667, 236)]
        [InlineData(ValueAxisRange.Auto, -10, "-15|-10|-5|0", 0, 157.333)]
        [InlineData(ValueAxisRange.Data, -10, "-15|-10|-5|0", 0, 157.333)]
        [InlineData(ValueAxisRange.IncludeZero, -10, "-15|-10|-5|0", 0, 157.333)]
        public async Task Column_EqualValues_ExtendFromZeroPastTheValueInEveryRange(ValueAxisRange range, double value, string labels, double top, double bottom)
        {
            using var ctx = CreateChartContext();

            var chart = await RenderAsync(ctx, p => p
                .AddChildContent<RadzenColumnSeries<DataItem>>(Column(Values(value, value, value)))
                .AddChildContent<RadzenValueAxis>(Axis(range)));

            Assert.Equal(labels, Labels(chart, LeftValueAxis));
            Assert.Equal((top, bottom), ColumnExtent(chart, 0));
        }

        [Fact]
        public async Task Column_DataRange_KeepsZeroThatRoundingReaches()
        {
            using var ctx = CreateChartContext();

            var chart = await RenderAsync(ctx, p => p
                .AddChildContent<RadzenColumnSeries<DataItem>>(Column(Values(10, 200, 450)))
                .AddChildContent<RadzenValueAxis>(Axis(ValueAxisRange.Data)));

            Assert.Equal("0|100|200|300|400|500", Labels(chart, LeftValueAxis));
            Assert.Equal((231.28, 236d), ColumnExtent(chart, 0));
        }

        [Fact]
        public async Task Column_ChangingRange_RerendersTheAxis()
        {
            using var ctx = CreateChartContext();

            var chart = await RenderAsync(ctx, p => p
                .AddChildContent<RadzenColumnSeries<DataItem>>(Column(Values(10, 20, 15)))
                .AddChildContent<RadzenValueAxis>(Axis(ValueAxisRange.Data)));

            Assert.Equal("10|15|20", Labels(chart, LeftValueAxis));

            var axis = chart.FindComponent<RadzenValueAxis>();

            axis.SetParametersAndRender(a => a.Add(x => x.Range, ValueAxisRange.Auto));

            Assert.Equal("0|5|10|15|20", Labels(chart, LeftValueAxis));
            Assert.Equal((118d, 236d), ColumnExtent(chart, 0));

            axis.SetParametersAndRender(a => a.Add(x => x.Range, ValueAxisRange.Data));

            Assert.Equal("10|15|20", Labels(chart, LeftValueAxis));
            Assert.Equal((236d, 236d), ColumnExtent(chart, 0));
        }

        [Fact]
        public async Task Line_ChangingRange_RerendersTheAxis()
        {
            using var ctx = CreateChartContext();

            var chart = await RenderAsync(ctx, p => p
                .AddChildContent<RadzenLineSeries<DataItem>>(Line(Values(10, 20, 15)))
                .AddChildContent<RadzenValueAxis>(Axis(ValueAxisRange.Auto)));

            Assert.Equal("10|15|20", Labels(chart, LeftValueAxis));

            chart.FindComponent<RadzenValueAxis>().SetParametersAndRender(a => a.Add(x => x.Range, ValueAxisRange.IncludeZero));

            Assert.Equal("0|5|10|15|20", Labels(chart, LeftValueAxis));
            Assert.Equal(118, FirstLinePointY(chart));
        }

        [Theory]
        [InlineData(ValueAxisRange.Auto, "10|15|20")]
        [InlineData(ValueAxisRange.Data, "10|15|20")]
        [InlineData(ValueAxisRange.IncludeZero, "0|5|10|15|20")]
        public async Task Candlestick_IncludesZeroOnlyWithIncludeZeroRange(ValueAxisRange range, string labels)
        {
            using var ctx = CreateChartContext();

            var data = new[]
            {
                new DataItem { Category = "A", Open = 12, High = 20, Low = 10, Close = 18 },
                new DataItem { Category = "B", Open = 18, High = 19, Low = 11, Close = 14 },
            };

            var chart = await RenderAsync(ctx, p => p
                .AddChildContent<RadzenCandlestickSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.OpenProperty, nameof(DataItem.Open))
                    .Add(x => x.HighProperty, nameof(DataItem.High))
                    .Add(x => x.LowProperty, nameof(DataItem.Low))
                    .Add(x => x.CloseProperty, nameof(DataItem.Close))
                    .Add(x => x.Data, data))
                .AddChildContent<RadzenValueAxis>(Axis(range)));

            Assert.Equal(labels, Labels(chart, LeftValueAxis));
        }
    
        private static IRenderedComponent<RadzenSparkline> RenderSparkline(TestContext ctx, string type, ValueAxisRange? range)
        {
            var data = Values(100, 120, 110, 115);

            return ctx.RenderComponent<RadzenSparkline>(p =>
            {
                p.Add(x => x.Style, "width: 200px; height: 40px");

                if (type == "area")
                {
                    p.AddChildContent<RadzenAreaSeries<DataItem>>(s => s
                        .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                        .Add(x => x.ValueProperty, nameof(DataItem.Value))
                        .Add(x => x.Data, data));
                }
                else
                {
                    p.AddChildContent<RadzenColumnSeries<DataItem>>(s => s
                        .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                        .Add(x => x.ValueProperty, nameof(DataItem.Value))
                        .Add(x => x.Data, data));
                }

                if (range != null)
                {
                    p.AddChildContent<RadzenValueAxis>(a => a.Add(x => x.Visible, false).Add(x => x.Range, range.Value));
                }
            });
        }

        [Theory]
        [InlineData("area", null)]
        [InlineData("area", ValueAxisRange.Auto)]
        [InlineData("column", null)]
        [InlineData("column", ValueAxisRange.Data)]
        public void Sparkline_FitsItsDataUnlessItsValueAxisIncludesZero(string type, ValueAxisRange? range)
        {
            using var ctx = CreateChartContext();

            var sparkline = RenderSparkline(ctx, type, range);

            var input = sparkline.Instance.ValueScale.Input;
            Assert.True(input.Start >= 90 && input.Start <= 100, $"value axis {input.Start}..{input.End}");
            Assert.True(input.End >= 120 && input.End <= 130, $"value axis {input.Start}..{input.End}");

            if (type == "area")
            {
                var line = Points(sparkline, "g.rz-area-series", 1);
                var output = sparkline.Instance.ValueScale.Output;
                Assert.True(line.Max(p => p.Y) - line.Min(p => p.Y) >= Math.Abs(output.Start - output.End) / 2, $"line {line.Min(p => p.Y)}..{line.Max(p => p.Y)} in {output.End}..{output.Start}");
            }
        }

        [Theory]
        [InlineData("area")]
        [InlineData("column")]
        public void Sparkline_IncludesZeroWhenItsValueAxisAsksForIt(string type)
        {
            using var ctx = CreateChartContext();

            var sparkline = RenderSparkline(ctx, type, ValueAxisRange.IncludeZero);

            Assert.Equal(0, sparkline.Instance.ValueScale.Input.Start);
        }
}
}
