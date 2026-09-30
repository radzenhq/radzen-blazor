using System;
using System.Globalization;
using System.Linq;
using AngleSharp.Dom;
using Bunit;
using Xunit;
using static Radzen.Blazor.Tests.ChartTestHelper;

namespace Radzen.Blazor.Tests
{
    public class ChartAxisClipTests
    {
        private static DataItem[] Data(params string[] categories)
        {
            return categories.Select((category, index) => new DataItem { Category = category, Value = 10 + index * 5 }).ToArray();
        }

        private static readonly DataItem[] LongNames = Data(
            "Regional distribution warehouse number 01",
            "\u4E0A\u6D77\u6D66\u4E1C\u65B0\u533A\u5F20\u6C5F\u9AD8\u79D1\u6280\u56ED\u533A\u7B2C\u4E8C\u7269\u6D41\u914D\u9001\u4E2D\u5FC3\u4ED3\u5E93\u7BA1\u7406\u90E8\u95E8\u534E\u4E1C\u533A\u57DF\u5206\u62E8\u4E2D\u5FC3\u603B\u90E8",
            "Headquarters finance and administration department");

        private static string AxisClipId(IRenderedComponent<RadzenChart> chart)
        {
            return chart.Instance.AxisClipPath;
        }

        private static void AssertClipsAtTheChartBounds(IRenderedComponent<RadzenChart> chart, double width, double height)
        {
            var transform = chart.FindAll("svg > g").Select(g => g.GetAttribute("transform") ?? string.Empty).First(t => t.StartsWith("translate(", StringComparison.Ordinal));
            var origin = transform.Replace("translate(", "", StringComparison.Ordinal).TrimEnd(')').Split(',')
                .Select(part => double.Parse(part, CultureInfo.InvariantCulture)).ToArray();
            var rect = chart.Find($"clipPath[id='{AxisClipId(chart)}'] rect");

            Assert.Equal(-origin[0], double.Parse(rect.GetAttribute("x"), CultureInfo.InvariantCulture), 6);
            Assert.Equal(-origin[1], double.Parse(rect.GetAttribute("y"), CultureInfo.InvariantCulture), 6);
            Assert.Equal(width, double.Parse(rect.GetAttribute("width"), CultureInfo.InvariantCulture), 6);
            Assert.Equal(height, double.Parse(rect.GetAttribute("height"), CultureInfo.InvariantCulture), 6);
        }

        private static bool IsClippedAtTheChartBounds(IRenderedComponent<RadzenChart> chart, IElement element)
        {
            for (var current = element; current != null; current = current.ParentElement)
            {
                if ((current.GetAttribute("style") ?? string.Empty).Contains($"url(#{AxisClipId(chart)})", StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        [Theory]
        [InlineData(CategoryAxisLabelFit.Auto)]
        [InlineData(CategoryAxisLabelFit.None)]
        public void ColumnChartClipsItsAxesAtTheChartBounds(CategoryAxisLabelFit fit)
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .Add(x => x.Style, "width: 390px; height: 300px")
                .AddChildContent<RadzenColumnSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Data, LongNames))
                .AddChildContent<RadzenCategoryAxis>(a => a.Add(x => x.LabelFit, fit)));

            AssertClipsAtTheChartBounds(chart, 390, 300);
            Assert.All(chart.FindAll("g.rz-axis"), axis => Assert.True(IsClippedAtTheChartBounds(chart, axis)));
            Assert.Equal(2, chart.FindAll("g.rz-axis").Count);
        }

        [Theory]
        [InlineData(CategoryAxisLabelFit.Auto)]
        [InlineData(CategoryAxisLabelFit.None)]
        public void BarChartClipsItsCategoryLabelsAtTheChartBounds(CategoryAxisLabelFit fit)
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .Add(x => x.Style, "width: 390px; height: 300px")
                .AddChildContent<RadzenBarSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Data, LongNames))
                .AddChildContent<RadzenCategoryAxis>(a => a.Add(x => x.LabelFit, fit)));

            AssertClipsAtTheChartBounds(chart, 390, 300);
            var labels = chart.FindAll("g.rz-value-axis .rz-tick-text");
            Assert.Equal(LongNames.Length, labels.Count);
            Assert.All(labels, label => Assert.True(IsClippedAtTheChartBounds(chart, label)));
            Assert.All(chart.FindAll("g.rz-category-axis .rz-tick-text"), label => Assert.True(IsClippedAtTheChartBounds(chart, label)));

            if (fit == CategoryAxisLabelFit.None)
            {
                Assert.Equal(LongNames.Select(d => d.Category), labels.Select(label => label.TextContent).Reverse());
            }
        }

        [Fact]
        public void AdditionalValueAxisIsClippedAtTheChartBounds()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .Add(x => x.Style, "width: 390px; height: 300px")
                .AddChildContent<RadzenColumnSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Data, Data("A", "B", "C")))
                .AddChildContent<RadzenLineSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value2))
                    .Add(x => x.ValueAxisName, "second")
                    .Add(x => x.Data, Data("A", "B", "C")))
                .AddChildContent<RadzenValueAxis>(a => a.Add(x => x.Name, "second")));

            var right = chart.Find("g.rz-value-axis-right");
            Assert.True(IsClippedAtTheChartBounds(chart, right));
        }

        [Fact]
        public void SeriesMarkersAndDataLabelsAreNotClippedAtTheChartBounds()
        {
            using var ctx = CreateChartContext();
            var data = Data("A", "B", "C", "D");

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .Add(x => x.Style, "width: 390px; height: 300px")
                .AddChildContent<RadzenLineSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Data, data)
                    .AddChildContent<RadzenMarkers>(m => m.Add(x => x.MarkerType, MarkerType.Circle))
                    .AddChildContent<RadzenSeriesDataLabels>(l => l.Add(x => x.Visible, true)))
                .AddChildContent<RadzenCategoryAxis>(a => a.Add(x => x.TickPlacement, TickPlacement.On)));

            var markers = chart.FindAll("g.rz-line-series g.rz-marker");
            var dataLabels = chart.FindAll(".rz-series-data-label");
            Assert.Equal(data.Length, markers.Count);
            Assert.Equal(data.Length, dataLabels.Count);
            Assert.All(markers, marker => Assert.False(IsClippedAtTheChartBounds(chart, marker)));
            Assert.All(dataLabels, label => Assert.False(IsClippedAtTheChartBounds(chart, label)));
            Assert.All(chart.FindAll("g.rz-line-series"), series => Assert.False(IsClippedAtTheChartBounds(chart, series)));
        }
    }
}
