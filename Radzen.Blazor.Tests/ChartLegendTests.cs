using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Xunit;
using static Radzen.Blazor.Tests.ChartTestHelper;

namespace Radzen.Blazor.Tests
{
    public class ChartLegendTests
    {
        private static RenderFragment TopLegendWithSeries(int count) => builder =>
        {
            builder.OpenComponent<RadzenLegend>(0);
            builder.AddAttribute(1, nameof(RadzenLegend.Position), LegendPosition.Top);
            builder.CloseComponent();

            for (var i = 0; i < count; i++)
            {
                builder.OpenComponent<RadzenLineSeries<DataItem>>(2);
                builder.AddAttribute(3, nameof(RadzenLineSeries<DataItem>.CategoryProperty), nameof(DataItem.Category));
                builder.AddAttribute(4, nameof(RadzenLineSeries<DataItem>.ValueProperty), nameof(DataItem.Value));
                builder.AddAttribute(5, nameof(RadzenLineSeries<DataItem>.Title), $"Long series title number {i}");
                builder.AddAttribute(6, nameof(RadzenLineSeries<DataItem>.Data), (IEnumerable<DataItem>)SampleData);
                builder.CloseComponent();
            }
        };

        private static double MarginTopOf(string markup)
        {
            var match = Regex.Match(markup, @"translate\([\d.]+,\s*([\d.]+)\)");
            Assert.True(match.Success, "Could not find plot translate transform in markup.");
            return double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
        }

        [Fact]
        public async Task TopLegend_WithManyWrappingSeries_ReservesMoreSpaceThanSingleRow()
        {
            using var ctx = CreateChartContext();

            var single = ctx.RenderComponent<RadzenChart>(p => p.Add(c => c.ChildContent, TopLegendWithSeries(1)));
            await single.InvokeAsync(() => single.Instance.Resize(200, 200));

            var many = ctx.RenderComponent<RadzenChart>(p => p.Add(c => c.ChildContent, TopLegendWithSeries(12)));
            await many.InvokeAsync(() => many.Instance.Resize(200, 200));

            Assert.True(MarginTopOf(many.Markup) > MarginTopOf(single.Markup),
                "A top legend with many wrapping series must reserve more vertical space than a single row.");
        }
    }
}
