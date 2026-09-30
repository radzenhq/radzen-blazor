using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using Radzen.Blazor.Rendering;
using Xunit;
using static Radzen.Blazor.Tests.ChartTestHelper;

namespace Radzen.Blazor.Tests
{
    public class ValueAxisLabelMeasureTests
    {
        private const string LeftValueAxis = "g.rz-value-axis:not(.rz-value-axis-right):not(.rz-value-axis-top)";
        private const string RightValueAxis = "g.rz-value-axis-right";

        public class WeatherItem
        {
            public DateTime Day { get; set; }
            public double Temperature { get; set; }
            public double Humidity { get; set; }
            public double Mixed { get; set; }
        }

        private static WeatherItem[] Weather()
        {
            var random = new Random(42);
            var start = new DateTime(2025, 1, 1);

            return Enumerable.Range(0, 120).Select(i => new WeatherItem
            {
                Day = start.AddDays(i),
                Temperature = Math.Round(8 + 12 * Math.Sin((i - 30) * Math.PI / 180) + random.NextDouble() * 6 - 3, 1),
                Humidity = Math.Round(55 + 15 * Math.Cos((i - 10) * Math.PI / 180) + random.NextDouble() * 10 - 5, 1)
            }).Select((item, i) =>
            {
                item.Mixed = i == 0 ? -1 : item.Humidity;
                return item;
            }).ToArray();
        }

        private static double OriginLeft(IRenderedComponent<RadzenChart> chart)
        {
            var transform = chart.FindAll("svg > g").Select(g => g.GetAttribute("transform") ?? string.Empty).First(t => t.StartsWith("translate(", StringComparison.Ordinal));

            return double.Parse(transform.Replace("translate(", "", StringComparison.Ordinal).Split(',')[0], CultureInfo.InvariantCulture);
        }

        private static string[] Labels(IRenderedComponent<RadzenChart> chart, string axis)
        {
            return chart.FindAll($"{axis} .rz-tick-text").Select(t => t.TextContent.Trim()).ToArray();
        }

        private static void AssertTheMarginFitsTheDrawnLabels(IRenderedComponent<RadzenChart> chart)
        {
            var labels = Labels(chart, LeftValueAxis);

            Assert.NotEmpty(labels);
            Assert.Equal(labels.Max(label => AxisMeasurer.ValueLabelWidth(label)) + 10, OriginLeft(chart), 6);
        }

        [Fact]
        public void LineChartMeasuresTheValueLabelsItDrawsWhenABottomLegendAppears()
        {
            using var ctx = CreateChartContext();
            var data = Weather();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .Add(x => x.Style, "width: 850px; height: 300px")
                .Add(x => x.AllowZoom, true)
                .Add(x => x.AllowPan, true)
                .AddChildContent<RadzenLineSeries<WeatherItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(WeatherItem.Day))
                    .Add(x => x.ValueProperty, nameof(WeatherItem.Temperature))
                    .Add(x => x.Title, "Temperature")
                    .Add(x => x.Data, data))
                .AddChildContent<RadzenLineSeries<WeatherItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(WeatherItem.Day))
                    .Add(x => x.ValueProperty, nameof(WeatherItem.Humidity))
                    .Add(x => x.Title, "Humidity")
                    .Add(x => x.Data, data))
                .AddChildContent<RadzenLegend>(l => l.Add(x => x.Position, LegendPosition.Bottom).Add(x => x.Visible, false)));

            Assert.Equal(new[] { "-20", "0", "20", "40", "60", "80" }, Labels(chart, LeftValueAxis));

            chart.FindComponent<RadzenLegend>().SetParametersAndRender(p => p.Add(x => x.Visible, true));

            Assert.Equal(new[] { "-50", "0", "50", "100" }, Labels(chart, LeftValueAxis));
            AssertTheMarginFitsTheDrawnLabels(chart);
        }

        [Fact]
        public void AdditionalValueAxisMeasuresTheLabelsItDrawsWhenABottomLegendAppears()
        {
            using var ctx = CreateChartContext();
            var data = Weather();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .Add(x => x.Style, "width: 850px; height: 300px")
                .Add(x => x.AllowZoom, true)
                .Add(x => x.AllowPan, true)
                .AddChildContent<RadzenLineSeries<WeatherItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(WeatherItem.Day))
                    .Add(x => x.ValueProperty, nameof(WeatherItem.Temperature))
                    .Add(x => x.Title, "Temperature")
                    .Add(x => x.Data, data))
                .AddChildContent<RadzenLineSeries<WeatherItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(WeatherItem.Day))
                    .Add(x => x.ValueProperty, nameof(WeatherItem.Mixed))
                    .Add(x => x.ValueAxisName, "mixed")
                    .Add(x => x.Title, "Mixed")
                    .Add(x => x.Data, data))
                .AddChildContent<RadzenValueAxis>(a => a.Add(x => x.Name, "mixed"))
                .AddChildContent<RadzenLegend>(l => l.Add(x => x.Position, LegendPosition.Bottom).Add(x => x.Visible, false)));

            Assert.Equal(new[] { "-20", "0", "20", "40", "60", "80" }, Labels(chart, RightValueAxis));

            chart.FindComponent<RadzenLegend>().SetParametersAndRender(p => p.Add(x => x.Visible, true));

            var labels = Labels(chart, RightValueAxis);
            Assert.Equal(new[] { "-50", "0", "50", "100" }, labels);
            Assert.Equal(850 - 32 - labels.Max(label => AxisMeasurer.ValueLabelWidth(label)) - 10, chart.Instance.CategoryScale.Output.End, 6);
        }

        [Theory]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(true, true)]
        [InlineData(false, false)]
        public async Task ValueAxisTakesItsMarginOnTheSideItIsDrawn(bool categoryInverted, bool rtl)
        {
            using var ctx = CreateChartContext();
            var data = new[] { 62.0, 71, 95, 84 }.Select((value, index) => new DataItem { Category = (2020 + index).ToString(CultureInfo.InvariantCulture), Value = value }).ToArray();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .Add(x => x.Style, "width: 875px; height: 400px")
                .AddChildContent<RadzenColumnSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Data, data))
                .AddChildContent<RadzenCategoryAxis>(a => a.Add(x => x.Inverted, categoryInverted))
                .AddChildContent<RadzenLegend>(l => l.Add(x => x.Visible, false)));

            await chart.InvokeAsync(() => chart.Instance.SetRTL(rtl));

            var labels = Labels(chart, "g.rz-value-axis");
            Assert.Equal("100", labels[^1]);
            var axisSize = labels.Max(label => AxisMeasurer.ValueLabelWidth(label)) + 10;
            var left = OriginLeft(chart);
            var right = 875 - left - chart.Instance.CategoryScale.OutputSize;
            var valueAxisRight = categoryInverted != rtl;
            Assert.Equal(valueAxisRight, chart.FindAll("g.rz-value-axis .rz-tick-text").All(text => text.GetAttribute("style") == "text-anchor: start"));
            Assert.Equal(valueAxisRight ? (32, axisSize) : (axisSize, 32), (left, right));
        }

        [Theory]
        [InlineData("100%", 36.34)]
        [InlineData("-4%", 25.42)]
        [InlineData("-50", 20.68)]
        [InlineData("$100,000", 58.39)]
        public void ValueLabelIsMeasuredWithinTheTickOffsetOfItsWidthInRoboto(string label, double renderedWidth)
        {
            Assert.True(AxisMeasurer.ValueLabelWidth(label) + 10 >= renderedWidth + 9, $"{label} measures {AxisMeasurer.ValueLabelWidth(label)}");
        }

        [Fact]
        public void PercentValueAxisReservesTheRenderedWidthOfItsLabels()
        {
            using var ctx = CreateChartContext();
            var data = new[] { 30.0, 45, 25 }.Select((value, index) => new DataItem { Category = ((char)('A' + index)).ToString(), Value = value }).ToArray();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .Add(x => x.Style, "width: 875px; height: 300px")
                .AddChildContent<RadzenFullStackedColumnSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Data, data))
                .AddChildContent<RadzenValueAxis>(a => a.Add(x => x.FormatString, "{0}%"))
                .AddChildContent<RadzenLegend>(l => l.Add(x => x.Visible, false)));

            Assert.Equal("100%", Labels(chart, LeftValueAxis)[^1]);
            Assert.True(OriginLeft(chart) >= 36.34 + 9, $"left margin {OriginLeft(chart)}");
        }
    }
}
