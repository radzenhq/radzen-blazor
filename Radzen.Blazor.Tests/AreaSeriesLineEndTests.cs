using System.Globalization;
using System.Linq;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Xunit;
using static Radzen.Blazor.Tests.ChartTestHelper;

namespace Radzen.Blazor.Tests
{
    public class AreaSeriesLineEndTests
    {
        static IRenderedComponent<RadzenChart> RenderChart(TestContext ctx, RenderFragment series) =>
            ctx.RenderComponent<RadzenChart>(p => p.AddChildContent(series));

        static RenderFragment Series<TSeries>(LineType lineType) where TSeries : IComponent => b =>
        {
            b.OpenComponent<TSeries>(0);
            b.AddAttribute(1, "Data", SampleData);
            b.AddAttribute(2, "CategoryProperty", nameof(DataItem.Category));
            if (typeof(TSeries) == typeof(RadzenRangeAreaSeries<DataItem>))
            {
                b.AddAttribute(3, "MinProperty", nameof(DataItem.Min));
                b.AddAttribute(4, "MaxProperty", nameof(DataItem.Max));
            }
            else
            {
                b.AddAttribute(3, "ValueProperty", nameof(DataItem.Value));
            }
            b.AddAttribute(5, "LineType", lineType);
            b.CloseComponent();
        };

        static double FirstX(IElement path) =>
            double.Parse(path.GetAttribute("d")!.Split(' ', System.StringSplitOptions.RemoveEmptyEntries)[1], CultureInfo.InvariantCulture);

        public static TheoryData<string> AreaSeriesTypes => new() { "area", "stacked", "fullstacked", "range" };

        static RenderFragment Pick(string kind, LineType lineType) => kind switch
        {
            "area" => Series<RadzenAreaSeries<DataItem>>(lineType),
            "stacked" => Series<RadzenStackedAreaSeries<DataItem>>(lineType),
            "fullstacked" => Series<RadzenFullStackedAreaSeries<DataItem>>(lineType),
            _ => Series<RadzenRangeAreaSeries<DataItem>>(lineType),
        };

        [Theory]
        [MemberData(nameof(AreaSeriesTypes))]
        public void SolidAreaStroke_EndsFlatAtTheFillEdge(string kind)
        {
            using var ctx = CreateChartContext();
            var chart = RenderChart(ctx, Pick(kind, LineType.Solid));

            var paths = chart.FindAll(".rz-series-0 > path");
            var fill = paths.First(p => p.GetAttribute("stroke") == "none");
            var strokes = paths.Where(p => p.GetAttribute("stroke") != "none").ToList();

            Assert.NotEmpty(strokes);
            Assert.All(strokes, s =>
            {
                Assert.Equal("butt", s.GetAttribute("stroke-linecap"));
            });
            Assert.Equal(FirstX(fill), strokes.Min(FirstX));
        }

        [Theory]
        [MemberData(nameof(AreaSeriesTypes))]
        public void DottedAreaStroke_KeepsRoundCapsForItsDots(string kind)
        {
            using var ctx = CreateChartContext();
            var chart = RenderChart(ctx, Pick(kind, LineType.Dotted));

            var strokes = chart.FindAll(".rz-series-0 > path").Where(p => p.GetAttribute("stroke") != "none").ToList();

            Assert.NotEmpty(strokes);
            Assert.All(strokes, s => Assert.Equal("round", s.GetAttribute("stroke-linecap")));
        }

        [Fact]
        public void SolidLineSeries_KeepsRoundCaps()
        {
            using var ctx = CreateChartContext();
            var chart = RenderChart(ctx, b =>
            {
                b.OpenComponent<RadzenLineSeries<DataItem>>(0);
                b.AddAttribute(1, "Data", SampleData);
                b.AddAttribute(2, "CategoryProperty", nameof(DataItem.Category));
                b.AddAttribute(3, "ValueProperty", nameof(DataItem.Value));
                b.CloseComponent();
            });

            Assert.Equal("round", chart.Find(".rz-line-series > path").GetAttribute("stroke-linecap"));
        }
    }
}
