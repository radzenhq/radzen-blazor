using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp.Html.Parser;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;
using Radzen.Blazor.Rendering;
using Xunit;
using static Radzen.Blazor.Tests.ChartTestHelper;

namespace Radzen.Blazor.Tests
{
    public class CategoryAxisLabelFitTests
    {
        private const string BottomAxis = "g.rz-category-axis";
        private const string LeftAxis = "g.rz-value-axis";
        private const string Ellipsis = "\u2026";

        private static string LongName(int index)
        {
            return $"Regional distribution warehouse number {index:00}";
        }

        private static DataItem[] Categories(params string[] names)
        {
            return names.Select((name, index) => new DataItem { Category = name, Value = 10 + index }).ToArray();
        }

        private static DataItem[] LongCategories(int count)
        {
            return Categories(Enumerable.Range(1, count).Select(LongName).ToArray());
        }

        private static IRenderedComponent<RadzenChart> Render(TestContext ctx, string style, params Action<ComponentParameterCollectionBuilder<RadzenChart>>[] content)
        {
            return ctx.RenderComponent<RadzenChart>(p =>
            {
                if (style != null)
                {
                    p.Add(x => x.Style, style);
                }

                foreach (var item in content)
                {
                    item(p);
                }
            });
        }

        private static Action<ComponentParameterCollectionBuilder<RadzenChart>> Series<TSeries>(DataItem[] data) where TSeries : CartesianSeries<DataItem>
        {
            return p => p.AddChildContent<TSeries>(s => s
                .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                .Add(x => x.ValueProperty, nameof(DataItem.Value))
                .Add(x => x.Data, data));
        }

        private static Action<ComponentParameterCollectionBuilder<RadzenChart>> Series(string type, DataItem[] data)
        {
            return type switch
            {
                "column" => Series<RadzenColumnSeries<DataItem>>(data),
                "line" => Series<RadzenLineSeries<DataItem>>(data),
                "bar" => Series<RadzenBarSeries<DataItem>>(data),
                _ => throw new ArgumentOutOfRangeException(nameof(type))
            };
        }

        private static Action<ComponentParameterCollectionBuilder<RadzenChart>> Axis(Action<ComponentParameterCollectionBuilder<RadzenCategoryAxis>> axis)
        {
            return p => p.AddChildContent<RadzenCategoryAxis>(axis);
        }

        private static Action<ComponentParameterCollectionBuilder<RadzenChart>> Fit(CategoryAxisLabelFit fit)
        {
            return Axis(a => a.Add(x => x.LabelFit, fit));
        }

        private static readonly Action<ComponentParameterCollectionBuilder<RadzenChart>> NoLegend =
            p => p.AddChildContent<RadzenLegend>(l => l.Add(x => x.Visible, false));

        private static (double Left, double Top) Origin(IRenderedComponent<RadzenChart> chart)
        {
            var transform = chart.FindAll("svg > g").Select(g => g.GetAttribute("transform") ?? string.Empty).First(t => t.StartsWith("translate(", StringComparison.Ordinal));
            var parts = transform.Replace("translate(", "", StringComparison.Ordinal).TrimEnd(')').Split(',');

            return (double.Parse(parts[0], CultureInfo.InvariantCulture), double.Parse(parts[1], CultureInfo.InvariantCulture));
        }

        private static double CategoryAxisLineY(IRenderedComponent<RadzenChart> chart)
        {
            var d = chart.Find($"{BottomAxis} > path.rz-line").GetAttribute("d") ?? string.Empty;

            return double.Parse(d.Split(' ')[2], CultureInfo.InvariantCulture);
        }

        private static void AssertBottomLabelsFitted(IRenderedComponent<RadzenChart> chart, int count, double height)
        {
            var texts = chart.FindAll($"{BottomAxis} .rz-tick-text");
            Assert.InRange(texts.Count, 1, count - 1);
            Assert.All(texts, text => Assert.StartsWith("rotate(-45,", text.GetAttribute("transform"), StringComparison.Ordinal));
            Assert.Equal(height, Origin(chart).Top + CategoryAxisLineY(chart) + chart.Instance.CategoryAxis.Size, 6);
        }

        private static void AssertLeftLabelsFitted(IRenderedComponent<RadzenChart> chart, double width)
        {
            var left = Origin(chart).Left;
            Assert.True(left <= width / 3, $"left margin {left}");
            Assert.Equal(chart.Instance.ValueAxis.Size, left, 6);
            Assert.All(Labels(chart, LeftAxis), label =>
            {
                Assert.EndsWith(Ellipsis, label, StringComparison.Ordinal);
                Assert.True(TextMeasurer.TextWidth(label) <= left - 10, label);
            });
        }

        private static List<string> Labels(IRenderedComponent<RadzenChart> chart, string axis)
        {
            return chart.FindAll($"{axis} .rz-tick-text").Select(t => t.TextContent.Trim()).ToList();
        }

        private static List<int> ShownIndices(IRenderedComponent<RadzenChart> chart, DataItem[] data)
        {
            return chart.FindAll($"{BottomAxis} g.rz-tick")
                .Select(tick => (tick.QuerySelector("title") ?? tick.QuerySelector(".rz-tick-text"))?.TextContent)
                .Where(text => text != null)
                .Select(text => Array.FindIndex(data, d => d.Category == text))
                .ToList();
        }

        private static List<string> Titles(IRenderedComponent<RadzenChart> chart, string axis)
        {
            return chart.FindAll($"{axis} g.rz-tick > title").Select(t => t.TextContent).ToList();
        }

        [Fact]
        public void VerticalAxisMeasuresTheLongestLabelAtAnOddIndex()
        {
            using var ctx = CreateChartContext();
            var longName = "An unusually long category name";

            var chart = Render(ctx, "width: 900px; height: 300px", NoLegend, Series("bar", Categories("A", longName, "B")), Fit(CategoryAxisLabelFit.None));

            Assert.True(Origin(chart).Left >= TextMeasurer.TextWidth(longName), $"left margin {Origin(chart).Left}");
        }

        [Fact]
        public void VerticalAxisIsCappedAtOneThirdOfTheChartWidth()
        {
            using var ctx = CreateChartContext();

            var chart = Render(ctx, "width: 300px; height: 300px", NoLegend, Series("bar", Categories("A", LongName(1), "B")));

            Assert.Equal(100, Origin(chart).Left, 6);
        }

        [Fact]
        public void VerticalAxisWidthWinsOverTheCap()
        {
            using var ctx = CreateChartContext();

            var chart = Render(ctx, "width: 300px; height: 300px", NoLegend, Series("bar", Categories("A", LongName(1), "B")),
                Axis(a => a.Add(x => x.Width, 150)));

            Assert.Equal(150, Origin(chart).Left, 6);
            var label = Labels(chart, LeftAxis)[1];
            Assert.EndsWith(Ellipsis, label, StringComparison.Ordinal);
            Assert.True(TextMeasurer.TextWidth(label) <= 150 - 10, label);
        }

        [Theory]
        [InlineData("column")]
        [InlineData("bar")]
        public void AxisSizeIsUnchangedWhenLabelsFit(string type)
        {
            using var ctx = CreateChartContext();
            var data = Categories("North", "South", "East", "West");

            var auto = Render(ctx, "width: 390px; height: 300px", Series(type, data));
            var none = Render(ctx, "width: 390px; height: 300px", Series(type, data), Fit(CategoryAxisLabelFit.None));

            Assert.Equal(Origin(none), Origin(auto));
            Assert.Equal(none.Instance.CategoryAxis.Size, auto.Instance.CategoryAxis.Size);
            Assert.Equal(none.Instance.ValueAxis.Size, auto.Instance.ValueAxis.Size);
        }

        [Fact]
        public void VerticalAxisKeepsItsNaturalWidthUnderNone()
        {
            using var ctx = CreateChartContext();
            var data = LongCategories(12);

            var chart = Render(ctx, "width: 900px; height: 300px", Series("bar", data), Fit(CategoryAxisLabelFit.None));

            Assert.Equal(data.Max(d => TextMeasurer.TextWidth(d.Category)) + 10, Origin(chart).Left, 6);
        }

        [Fact]
        public void RotatedBandIsCappedAtOneThirdOfTheChartHeight()
        {
            using var ctx = CreateChartContext();

            var data = Categories(Enumerable.Range(1, 6).Select(i => $"Regional-distribution-warehouse-number-{i:00}").ToArray());

            var chart = Render(ctx, "width: 400px; height: 300px", NoLegend, Series("column", data));

            var size = chart.Instance.CategoryAxis.Size;
            Assert.InRange(size, 90, 100);
        }

        [Fact]
        public void MissingCharacterCountsAsTheAverageWidth()
        {
            var average = TextMeasurer.TextWidth("\u2026");

            Assert.True(average > 0);
            Assert.Equal(2 * average, TextMeasurer.TextWidth("\u00E9\u03A9"), 6);
            Assert.Equal(TextMeasurer.TextWidth("a") + average, TextMeasurer.TextWidth("a\u00E9"), 6);
        }

        [Theory]
        [InlineData("\u4E2D")]
        [InlineData("\u3042")]
        [InlineData("\u30AB")]
        [InlineData("\uAC00")]
        [InlineData("\uFF21")]
        [InlineData("\U00020000")]
        public void WideCharacterCountsAsOneEm(string character)
        {
            Assert.Equal(14, TextMeasurer.TextWidth(character), 6);
            Assert.Equal(20, TextMeasurer.TextWidth(character, 20), 6);
            Assert.Equal(TextMeasurer.TextWidth("a") + 14, TextMeasurer.TextWidth("a" + character), 6);
        }

        [Theory]
        [InlineData("column")]
        [InlineData("line")]
        public void ShortLabelsRenderTheSameMarkupUnderAutoAndNone(string type)
        {
            using var ctx = CreateChartContext();
            var data = Categories("North", "South", "East", "West");

            var auto = Render(ctx, "width: 390px; height: 300px", Series(type, data));
            var none = Render(ctx, "width: 390px; height: 300px", Series(type, data), Fit(CategoryAxisLabelFit.None));

            Assert.Equal(none.Find(BottomAxis).InnerHtml, auto.Find(BottomAxis).InnerHtml);
            Assert.Equal(none.Find(LeftAxis).InnerHtml, auto.Find(LeftAxis).InnerHtml);
            Assert.Equal(Origin(none), Origin(auto));
            Assert.Equal(new[] { "North", "South", "East", "West" }, Labels(auto, BottomAxis));
        }

        [Theory]
        [InlineData("column")]
        [InlineData("line")]
        public void LongLabelsRotateByMinus45WithTextAnchorEnd(string type)
        {
            using var ctx = CreateChartContext();

            var chart = Render(ctx, "width: 900px; height: 400px", Series(type, LongCategories(8)));

            var texts = chart.FindAll($"{BottomAxis} .rz-tick-text");
            Assert.NotEmpty(texts);
            Assert.All(texts, text =>
            {
                Assert.StartsWith("rotate(-45,", text.GetAttribute("transform"), StringComparison.Ordinal);
                Assert.Equal("end", text.GetAttribute("text-anchor"));
            });
        }

        [Fact]
        public void CrowdedLabelsKeepTheFirstAndARegularStride()
        {
            using var ctx = CreateChartContext();
            var data = Categories(Enumerable.Range(1, 30).Select(i => $"Category {i:00}").ToArray());

            var chart = Render(ctx, "width: 300px; height: 300px", Series("column", data));

            var shown = ShownIndices(chart, data);
            Assert.Equal(0, shown[0]);
            var stride = shown[1] - shown[0];
            Assert.True(stride > 1, $"stride {stride}");
            var expected = Enumerable.Range(0, 30).Where(i => i % stride == 0).ToList();
            Assert.Equal(expected, shown);
            Assert.Equal(shown.Count, chart.FindAll($"{BottomAxis} g.rz-tick").Count);
        }

        [Fact]
        public void CrowdedLabelsKeepTheLastOnlyWhenItFallsOnTheStride()
        {
            using var ctx = CreateChartContext();
            var keptLast = 0;

            for (var count = 24; count <= 36; count++)
            {
                var data = Categories(Enumerable.Range(1, count).Select(i => $"Category {i:00}").ToArray());

                var chart = Render(ctx, "width: 300px; height: 300px", Series("column", data));

                var shown = ShownIndices(chart, data);
                var stride = shown[1] - shown[0];
                var last = count - 1;
                Assert.Equal(last % stride == 0, shown[^1] == last);
                keptLast += shown[^1] == last ? 1 : 0;
            }

            Assert.True(keptLast > 0);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task RotatedLabelsDoNotCrossTheLeftEdgeOfTheChart(bool rtl)
        {
            using var ctx = CreateChartContext();
            var data = LongCategories(12);

            var chart = Render(ctx, "width: 390px; height: 360px", Series("column", data));
            await chart.InvokeAsync(() => chart.Instance.SetRTL(rtl));

            var left = Origin(chart).Left;
            var ticks = chart.FindAll($"{BottomAxis} g.rz-tick").Where(tick => tick.QuerySelector(".rz-tick-text") != null).ToList();
            Assert.NotEmpty(ticks);
            Assert.All(ticks, tick =>
            {
                var text = tick.QuerySelector(".rz-tick-text");
                var x = double.Parse(text.GetAttribute("x"), CultureInfo.InvariantCulture);
                Assert.True(TextMeasurer.TextWidth(text.TextContent) * Math.Cos(Math.PI / 4) <= left + x + 1e-9, $"{text.TextContent} at {left + x}");
                Assert.Equal(text.TextContent.EndsWith(Ellipsis, StringComparison.Ordinal), tick.QuerySelector("title") != null);
            });
            if (!rtl)
            {
                var first = ticks[0].QuerySelector(".rz-tick-text").TextContent;
                Assert.Equal(data[0].Category, ticks[0].QuerySelector("title").TextContent);
                Assert.True(TextMeasurer.TextWidth(first) < ticks.Max(tick => TextMeasurer.TextWidth(tick.QuerySelector(".rz-tick-text").TextContent)), first);
            }
        }

        private static List<(string Text, string Transform, string Anchor)> RenderedLabels(IRenderedComponent<RadzenChart> chart)
        {
            return chart.FindAll($"{BottomAxis} .rz-tick-text").Select(t => (t.TextContent, t.GetAttribute("transform"), t.GetAttribute("text-anchor"))).ToList();
        }

        [Fact]
        public void LabelRotationSetsTheAngleWhileAutoStillSkipsAndShortens()
        {
            using var ctx = CreateChartContext();
            var data = LongCategories(12);

            var chart = Render(ctx, "width: 390px; height: 300px", Series("column", data), Axis(a => a.Add(x => x.LabelRotation, 30)));

            var labels = RenderedLabels(chart);
            Assert.InRange(labels.Count, 1, 11);
            Assert.All(labels, label =>
            {
                Assert.StartsWith("rotate(30,", label.Transform, StringComparison.Ordinal);
                Assert.Equal("start", label.Anchor);
                Assert.EndsWith(Ellipsis, label.Text, StringComparison.Ordinal);
            });
            Assert.Equal(labels.Count, Titles(chart, BottomAxis).Count);
        }

        [Fact]
        public void LabelRotationRotatesLabelsThatFit()
        {
            using var ctx = CreateChartContext();

            var chart = Render(ctx, "width: 390px; height: 300px", Series("column", Categories("North", "South", "East", "West")),
                Axis(a => a.Add(x => x.LabelRotation, -30)));

            var labels = RenderedLabels(chart);
            Assert.Equal(new[] { "North", "South", "East", "West" }, labels.Select(label => label.Text));
            Assert.All(labels, label => Assert.StartsWith("rotate(-30,", label.Transform, StringComparison.Ordinal));
            Assert.Empty(Titles(chart, BottomAxis));
        }

        [Fact]
        public void PositiveLabelRotationKeepsLabelsInsideTheRightEdgeOfTheChart()
        {
            using var ctx = CreateChartContext();
            var data = LongCategories(12);

            var chart = Render(ctx, "width: 390px; height: 360px", NoLegend, Series("column", data), Axis(a => a.Add(x => x.LabelRotation, 45)));

            var left = Origin(chart).Left;
            var texts = chart.FindAll($"{BottomAxis} .rz-tick-text");
            Assert.NotEmpty(texts);
            Assert.All(texts, text =>
            {
                var x = double.Parse(text.GetAttribute("x"), CultureInfo.InvariantCulture);
                Assert.True(left + x + TextMeasurer.TextWidth(text.TextContent) * Math.Cos(Math.PI / 4) <= 390 + 1e-9, $"{text.TextContent} at {left + x}");
            });
        }

        [Fact]
        public void ZeroLabelRotationSkipsOverlappingHorizontalLabels()
        {
            using var ctx = CreateChartContext();
            var data = Categories(Enumerable.Range(1, 30).Select(i => $"Category {i:00}").ToArray());

            var chart = Render(ctx, "width: 390px; height: 300px", Series("column", data), Axis(a => a.Add(x => x.LabelRotation, 0)));

            var labels = RenderedLabels(chart);
            Assert.InRange(labels.Count, 2, 29);
            Assert.All(labels, label =>
            {
                Assert.StartsWith("rotate(0,", label.Transform, StringComparison.Ordinal);
                Assert.Equal("middle", label.Anchor);
            });
            var positions = chart.FindAll($"{BottomAxis} .rz-tick-text").Select(t => double.Parse(t.GetAttribute("x"), CultureInfo.InvariantCulture)).ToList();
            for (var index = 1; index < positions.Count; index++)
            {
                Assert.True(positions[index] - positions[index - 1] >= TextMeasurer.TextWidth(labels[index].Text), $"{labels[index - 1].Text} and {labels[index].Text}");
            }
        }

        [Fact]
        public void LabelAutoRotationSetsTheAngleWhileAutoStillSkips()
        {
            using var ctx = CreateChartContext();
            var data = LongCategories(24);

            var auto = Render(ctx, "width: 390px; height: 300px", Series("column", data),
                Axis(a => a.Add(x => x.LabelAutoRotation, -60)));
            var none = Render(ctx, "width: 390px; height: 300px", Series("column", data),
                Axis(a => a.Add(x => x.LabelAutoRotation, -60).Add(x => x.LabelFit, CategoryAxisLabelFit.None)));

            var fitted = RenderedLabels(auto);
            Assert.InRange(fitted.Count, 1, 23);
            Assert.All(fitted, label => Assert.StartsWith("rotate(-60,", label.Transform, StringComparison.Ordinal));
            Assert.Equal(fitted.Count(label => label.Text.EndsWith(Ellipsis, StringComparison.Ordinal)), Titles(auto, BottomAxis).Count);

            var full = RenderedLabels(none);
            Assert.Equal(data.Select(d => d.Category), full.Select(label => label.Text));
            Assert.All(full, label => Assert.StartsWith("rotate(-60,", label.Transform, StringComparison.Ordinal));
            Assert.Empty(Titles(none, BottomAxis));
        }

        [Fact]
        public void LabelAutoRotationLeavesLabelsThatFitUnrotated()
        {
            using var ctx = CreateChartContext();
            var data = Categories("North", "South", "East", "West");

            var rotation = Render(ctx, "width: 390px; height: 300px", Series("column", data), Axis(a => a.Add(x => x.LabelAutoRotation, -45)));
            var plain = Render(ctx, "width: 390px; height: 300px", Series("column", data));

            Assert.Equal(plain.Find(BottomAxis).InnerHtml, rotation.Find(BottomAxis).InnerHtml);
            Assert.Equal(Origin(plain), Origin(rotation));
            Assert.Equal(new[] { "North", "South", "East", "West" }, Labels(rotation, BottomAxis));
            Assert.All(RenderedLabels(rotation), label => Assert.Null(label.Transform));
        }

        [Fact]
        public void AreaDemoMonthsWithLabelAutoRotationAreSkippedAndStayFlatAtPhoneWidth()
        {
            using var ctx = CreateChartContext();
            var months = new[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec" };
            var data = Categories(months);

            var auto = Render(ctx, "width: 324px; height: 300px", Series("column", data), Axis(a => a.Add(x => x.LabelAutoRotation, -45)));
            var none = Render(ctx, "width: 324px; height: 300px", Series("column", data),
                Axis(a => a.Add(x => x.LabelAutoRotation, -45).Add(x => x.LabelFit, CategoryAxisLabelFit.None)));

            var shown = ShownIndices(auto, data);
            Assert.InRange(shown.Count, 2, 11);
            Assert.Equal(Enumerable.Range(0, 12).Where(i => i % shown[1] == 0), shown);
            Assert.All(RenderedLabels(auto), label => Assert.Null(label.Transform));
            Assert.Equal(months, Labels(none, BottomAxis));
        }

        [Theory]
        [InlineData(324, new[] { 0, 3, 6, 9 })]
        [InlineData(850, new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 })]
        public async Task AreaDemoMonthsSkipBeforeRotating(double width, int[] expected)
        {
            using var ctx = CreateChartContext();
            var months = new[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec" };
            var data = months.Select((month, index) => new DataItem { Category = month, Value = 234000 + index * 10000 }).ToArray();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenAreaSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Title, "2023")
                    .Add(x => x.Data, data))
                .AddChildContent<RadzenAreaSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value2))
                    .Add(x => x.Title, "2024")
                    .Add(x => x.Data, data))
                .AddChildContent<RadzenValueAxis>(a => a
                    .Add(x => x.Formatter, value => ((double)value).ToString("C0", CultureInfo.CreateSpecificCulture("en-US")))
                    .AddChildContent<RadzenAxisTitle>(t => t.Add(x => x.Text, "Revenue in USD"))));
            await chart.InvokeAsync(() => chart.Instance.Resize(width, 300));

            Assert.Equal(expected, ShownIndices(chart, data));
            Assert.All(RenderedLabels(chart), label => Assert.Null(label.Transform));
        }

        [Theory]
        [InlineData(390, new[] { 0, 2, 4, 6, 8, 10 })]
        [InlineData(900, new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 })]
        public void LongLabelsThatDoNotFitFlatAfterSkippingRotate(double width, int[] expected)
        {
            using var ctx = CreateChartContext();
            var data = LongCategories(12);

            var chart = Render(ctx, FormattableString.Invariant($"width: {width}px; height: 360px"), Series("column", data));

            Assert.Equal(expected, ShownIndices(chart, data));
            Assert.All(RenderedLabels(chart), label => Assert.StartsWith("rotate(-45,", label.Transform, StringComparison.Ordinal));
        }

        [Theory]
        [InlineData("Regional distribution warehouse number 01", "Regional distrib", "Regional\u2026")]
        [InlineData("Regional distribution warehouse number 01", "Regional distribution", "Regional distribution\u2026")]
        [InlineData("Regional distribution warehouse number 01", "Regional distribution w", "Regional distribution\u2026")]
        [InlineData("Supercalifragilisticexpialidocious", "Supercalifrag", "Supercalifrag\u2026")]
        [InlineData("\u4E0A\u6D77\u6D66\u4E1C\u65B0\u533A\u5F20\u6C5F", "\u4E0A\u6D77\u6D66\u4E1C", "\u4E0A\u6D77\u6D66\u4E1C\u2026")]
        [InlineData("Short", "Short", "Short")]
        public void ShortenCutsAfterTheLastWholeWordThatFits(string text, string fitting, string expected)
        {
            Assert.Equal(expected, AxisMeasurer.Shorten(text, TextMeasurer.TextWidth(fitting == text ? text : fitting + Ellipsis)));
        }

        [Theory]
        [InlineData("Cambodia, Laos and Vietnam", "Cambodia, La", "Cambodia\u2026")]
        [InlineData("Research - development and testing", "Research - develo", "Research\u2026")]
        [InlineData("North; South and East", "North; Sou", "North\u2026")]
        [InlineData("Sales (EU) and exports", "Sales (EU) an", "Sales (EU)\u2026")]
        [InlineData("Revenue \"net\" of taxes", "Revenue \"net\" o", "Revenue \"net\"\u2026")]
        [InlineData("Growth 5% in the first quarter", "Growth 5% i", "Growth 5%\u2026")]
        [InlineData("Sales & marketing", "Sales & mar", "Sales\u2026")]
        [InlineData(", , , , , ,", ", , ,", ", , ,\u2026")]
        public void ShortenDropsTrailingPunctuationBeforeTheEllipsis(string text, string fitting, string expected)
        {
            Assert.Equal(expected, AxisMeasurer.Shorten(text, TextMeasurer.TextWidth(fitting + Ellipsis)));
        }

        [Fact]
        public void ShortenedBarLabelDoesNotEndWithACommaBeforeTheEllipsis()
        {
            using var ctx = CreateChartContext();
            var category = "Cambodia, Myanmar-Thailand border region";

            var chart = Render(ctx, "width: 390px; height: 300px", NoLegend, Series("bar", Categories("North", category, "South")));

            Assert.Equal(new[] { "South", "Cambodia\u2026", "North" }, Labels(chart, LeftAxis));
            Assert.Equal(new[] { category }, Titles(chart, LeftAxis));
        }

        [Fact]
        public void ShortenedBarLabelsEndWithAWholeWord()
        {
            using var ctx = CreateChartContext();
            var data = LongCategories(12);

            var chart = Render(ctx, "width: 390px; height: 360px", Series("bar", data));

            var labels = Labels(chart, LeftAxis);
            Assert.All(labels, label =>
            {
                Assert.EndsWith(Ellipsis, label, StringComparison.Ordinal);
                var kept = label[..^1];
                var full = data.Select(d => d.Category).First(category => category.StartsWith(kept, StringComparison.Ordinal));
                Assert.True(full.Length > kept.Length && char.IsWhiteSpace(full[kept.Length]), $"{label} cuts {full} mid-word");
            });
        }

        [Fact]
        public void TickTemplateReceivesEveryCategoryWithoutTitle()
        {
            using var ctx = CreateChartContext();
            var data = LongCategories(30);

            var chart = Render(ctx, "width: 300px; height: 300px", Series("column", data),
                Axis(a => a.AddChildContent<RadzenTicks>(t => t.Add(x => x.Template, context => $"<text class=\"custom-tick\">{context.Text}</text>"))));

            Assert.Equal(data.Select(d => d.Category), chart.FindAll($"{BottomAxis} .custom-tick").Select(t => t.TextContent));
            Assert.Empty(chart.FindAll($"{BottomAxis} title"));
            Assert.Equal(16 * 0.875 + 12, chart.Instance.CategoryAxis.Size, 6);
        }

        [Fact]
        public void FormatterReturningEmptyStillDropsTheTick()
        {
            using var ctx = CreateChartContext();

            var chart = Render(ctx, "width: 390px; height: 300px", Series("column", Categories("North", "South", "East", "West")),
                Axis(a => a.Add(x => x.Formatter, value => (string)value == "East" ? "" : (string)value)));

            Assert.Equal(new[] { "North", "South", "West" }, Labels(chart, BottomAxis));
            Assert.Equal(3, chart.FindAll($"{BottomAxis} g.rz-tick").Count);
        }

        [Fact]
        public void RotatedLabelLongerThanTheCapIsShortenedWithTitle()
        {
            using var ctx = CreateChartContext();
            var longName = "Regional distribution warehouse and logistics center of the northern district";

            var chart = Render(ctx, "width: 400px; height: 300px", Series("column", Categories("North", "South", longName, "West")));

            var labels = Labels(chart, BottomAxis);
            Assert.Equal(4, labels.Count);
            Assert.EndsWith(Ellipsis, labels[2], StringComparison.Ordinal);
            Assert.StartsWith("Regional", labels[2], StringComparison.Ordinal);
            Assert.Equal(new[] { longName }, Titles(chart, BottomAxis));
            Assert.Equal("title", chart.FindAll($"{BottomAxis} g.rz-tick")[2].FirstElementChild?.LocalName);
        }

        [Fact]
        public void VerticalLabelsAreShortenedToTheCapWithTitles()
        {
            using var ctx = CreateChartContext();
            var data = LongCategories(12);

            var chart = Render(ctx, "width: 390px; height: 300px", Series("bar", data));

            var left = Origin(chart).Left;
            Assert.True(left <= 390.0 / 3, $"left margin {left}");
            var labels = Labels(chart, LeftAxis);
            Assert.Equal(12, labels.Count);
            Assert.All(labels, label =>
            {
                Assert.EndsWith(Ellipsis, label, StringComparison.Ordinal);
                Assert.True(TextMeasurer.TextWidth(label) <= left - 10, label);
            });
            Assert.Equal(data.Select(d => d.Category).Reverse(), Titles(chart, LeftAxis));
        }

        private static readonly string[] CashFlowCategories = { "Revenue", "Services", "COGS", "Gross Profit", "Salaries", "Marketing", "R&D", "Net Income" };

        private static IRenderedComponent<RadzenChart> RenderBarsWithAxisTitle(TestContext ctx, double width, string seriesTitle, bool legend)
        {
            return ctx.RenderComponent<RadzenChart>(p =>
            {
                p.Add(x => x.Style, FormattableString.Invariant($"width: {width}px; height: 300px"));
                p.AddChildContent<RadzenBarSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Title, seriesTitle)
                    .Add(x => x.Data, Categories(CashFlowCategories)));
                p.AddChildContent<RadzenCategoryAxis>(a => a.AddChildContent<RadzenAxisTitle>(t => t.Add(x => x.Text, "Amount ($)")));
                p.AddChildContent<RadzenLegend>(l => l.Add(x => x.Visible, legend));
            });
        }

        private static double PlotWidth(IRenderedComponent<RadzenChart> chart)
        {
            var output = chart.Instance.CategoryScale.Output;

            return Math.Abs(output.End - output.Start);
        }

        private const double TitledAxisPadding = 10 + 16 * 0.875 + 32;

        [Fact]
        public void AxisTitleDoesNotTakeTheRoomOfTheBarLabels()
        {
            using var ctx = CreateChartContext();

            var chart = RenderBarsWithAxisTitle(ctx, 324, "Cash Flow", true);

            var room = Origin(chart).Left - TitledAxisPadding;
            var legendWidth = chart.Instance.Legend.Measure(chart.Instance) + 16;
            Assert.Equal((324 - legendWidth) / 3 - 10, room, 6);
            Assert.True(PlotWidth(chart) >= 80, $"plot width {PlotWidth(chart)}");
            var labels = Labels(chart, LeftAxis);
            Assert.Equal(CashFlowCategories.Reverse(), labels.Select(label => CashFlowCategories.First(category => category == label || (label.EndsWith(Ellipsis, StringComparison.Ordinal) && category.StartsWith(label[..^1], StringComparison.Ordinal)))));
            Assert.All(labels, label =>
            {
                Assert.True(TextMeasurer.TextWidth(label) <= room, label);
                Assert.True(label.TrimEnd(Ellipsis[0]).Length >= 3, label);
            });
        }

        [Fact]
        public void BarLabelsKeepAReadableMinimumWhenAThirdOfTheWidthIsLess()
        {
            using var ctx = CreateChartContext();

            var chart = RenderBarsWithAxisTitle(ctx, 400, "Operating cash flow of every department", true);

            var legendWidth = chart.Instance.Legend.Measure(chart.Instance) + 16;
            Assert.True((400 - legendWidth) / 3 - 10 < 40, $"legend width {legendWidth}");
            Assert.Equal(40, Origin(chart).Left - TitledAxisPadding, 6);
            Assert.True(PlotWidth(chart) >= 80, $"plot width {PlotWidth(chart)}");
            Assert.All(Labels(chart, LeftAxis), label =>
            {
                Assert.True(TextMeasurer.TextWidth(label) <= 40, label);
                Assert.True(label.TrimEnd(Ellipsis[0]).Length >= 3, label);
            });
        }

        [Fact]
        public void BarLabelsAreCutBelowTheReadableMinimumOnlyAtThePlotFloor()
        {
            using var ctx = CreateChartContext();

            var chart = RenderBarsWithAxisTitle(ctx, 180, "Cash Flow", false);

            Assert.Equal(80, PlotWidth(chart), 6);
            var room = Origin(chart).Left - TitledAxisPadding;
            Assert.True(room < 40, $"room {room}");
            Assert.All(Labels(chart, LeftAxis), label => Assert.True(TextMeasurer.TextWidth(label) <= room, label));
        }

        [Fact]
        public void VerticalLabelsKeepFullTextUnderNone()
        {
            using var ctx = CreateChartContext();
            var data = LongCategories(12);

            var chart = Render(ctx, "width: 390px; height: 300px", Series("bar", data), Fit(CategoryAxisLabelFit.None));

            Assert.Equal(data.Select(d => d.Category).Reverse(), Labels(chart, LeftAxis));
            Assert.Empty(Titles(chart, LeftAxis));
        }

        [Fact]
        public void ShortVerticalLabelsHaveNoTitle()
        {
            using var ctx = CreateChartContext();

            var chart = Render(ctx, "width: 390px; height: 300px", Series("bar", Categories("North", "South", "East")));

            Assert.Equal(new[] { "East", "South", "North" }, Labels(chart, LeftAxis));
            Assert.Empty(chart.FindAll("title"));
        }

        [Fact]
        public void ShortenedBarKeepsTheFullCategoryInItsTooltip()
        {
            using var ctx = CreateChartContext();
            var data = LongCategories(12);

            var chart = Render(ctx, "width: 390px; height: 300px", Series("bar", data));
            Assert.EndsWith(Ellipsis, Labels(chart, LeftAxis)[0], StringComparison.Ordinal);

            var series = chart.FindComponent<RadzenBarSeries<DataItem>>().Instance;
            var tooltip = ctx.Render(series.RenderTooltip(data[0]));

            Assert.Equal(data[0].Category, tooltip.Find(".rz-chart-tooltip-title").TextContent);
        }

        [Fact]
        public void LabelsAreNotFittedBeforeTheChartKnowsItsSize()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var createChart = ctx.JSInterop.Setup<Rect>("Radzen.createChart", _ => true);
            ctx.Services.AddScoped<TooltipService>();
            var data = LongCategories(30);

            var chart = Render(ctx, null, Series("column", data));

            Assert.Null(chart.Instance.CategoryAxis.LabelLayout);
            Assert.DoesNotContain(Ellipsis, chart.Markup, StringComparison.Ordinal);
            Assert.Empty(chart.FindAll("title"));

            createChart.SetResult(new Rect { Width = 300, Height = 300 });

            chart.WaitForAssertion(() => Assert.InRange(Labels(chart, BottomAxis).Count, 1, data.Length - 1));
        }

        [Theory]
        [InlineData("column", false)]
        [InlineData("column", true)]
        [InlineData("bar", false)]
        [InlineData("bar", true)]
        public void LabelsAreFittedOnTheFirstRenderWhenTheAxisFollowsTheSeries(string type, bool explicitAuto)
        {
            using var ctx = CreateChartContext();
            var data = LongCategories(12);

            var chart = Render(ctx, "width: 390px; height: 360px", Series(type, data),
                explicitAuto ? Fit(CategoryAxisLabelFit.Auto) : Axis(a => { }));

            if (type == "bar")
            {
                AssertLeftLabelsFitted(chart, 390);
            }
            else
            {
                AssertBottomLabelsFitted(chart, data.Length, 360);
            }
        }

        [Theory]
        [InlineData("column")]
        [InlineData("bar")]
        public void LabelsAreFittedWhenTheSizeArrivesAfterTheFirstRender(string type)
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var createChart = ctx.JSInterop.Setup<Rect>("Radzen.createChart", _ => true);
            ctx.Services.AddScoped<TooltipService>();
            var data = LongCategories(12);

            var chart = Render(ctx, null, Series(type, data), Fit(CategoryAxisLabelFit.Auto));

            createChart.SetResult(new Rect { Width = 390, Height = 360 });

            chart.WaitForAssertion(() =>
            {
                if (type == "bar")
                {
                    AssertLeftLabelsFitted(chart, 390);
                }
                else
                {
                    AssertBottomLabelsFitted(chart, data.Length, 360);
                }
            });
        }

        [Fact]
        public async Task StaticPrerenderWithPixelSizeRendersFittedLabels()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IJSRuntime, PrerenderJSRuntime>();
            services.AddSingleton<NavigationManager, TestNavigationManager>();
            services.AddScoped<TooltipService>();
            await using var provider = services.BuildServiceProvider();
            await using var scope = provider.CreateAsyncScope();
            await using var renderer = new HtmlRenderer(scope.ServiceProvider, NullLoggerFactory.Instance);
            var data = LongCategories(12);

            RenderFragment content = builder =>
            {
                builder.OpenComponent<RadzenColumnSeries<DataItem>>(0);
                builder.AddAttribute(1, nameof(RadzenColumnSeries<DataItem>.Data), data);
                builder.AddAttribute(2, nameof(RadzenColumnSeries<DataItem>.CategoryProperty), nameof(DataItem.Category));
                builder.AddAttribute(3, nameof(RadzenColumnSeries<DataItem>.ValueProperty), nameof(DataItem.Value));
                builder.CloseComponent();
                builder.OpenComponent<RadzenCategoryAxis>(4);
                builder.CloseComponent();
            };

            var html = await renderer.Dispatcher.InvokeAsync(async () =>
            {
                var output = await renderer.RenderComponentAsync<RadzenChart>(ParameterView.FromDictionary(new Dictionary<string, object>
                {
                    [nameof(RadzenChart.Style)] = "width: 390px; height: 360px",
                    [nameof(RadzenChart.ChildContent)] = content
                }));

                return output.ToHtmlString();
            });

            var document = new HtmlParser().ParseDocument(html);
            var texts = document.QuerySelectorAll($"{BottomAxis} .rz-tick-text");
            Assert.InRange(texts.Length, 1, data.Length - 1);
            Assert.All(texts, text => Assert.StartsWith("rotate(-45,", text.GetAttribute("transform"), StringComparison.Ordinal));
        }

        private sealed class PrerenderJSRuntime : IJSRuntime
        {
            public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object[] args) =>
                throw new InvalidOperationException("JavaScript interop calls cannot be issued at this time. This is because the component is being statically rendered.");

            public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object[] args) =>
                InvokeAsync<TValue>(identifier, args);
        }

        private sealed class TestNavigationManager : NavigationManager
        {
            public TestNavigationManager()
            {
                Initialize("http://localhost/", "http://localhost/");
            }
        }
    }
}
