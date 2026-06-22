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

        private static RenderFragment LegendWithSeries(LegendPosition position) => builder =>
        {
            builder.OpenComponent<RadzenLegend>(0);
            builder.AddAttribute(1, nameof(RadzenLegend.Position), position);
            builder.CloseComponent();

            builder.OpenComponent<RadzenLineSeries<DataItem>>(2);
            builder.AddAttribute(3, nameof(RadzenLineSeries<DataItem>.CategoryProperty), nameof(DataItem.Category));
            builder.AddAttribute(4, nameof(RadzenLineSeries<DataItem>.ValueProperty), nameof(DataItem.Value));
            builder.AddAttribute(5, nameof(RadzenLineSeries<DataItem>.Title), "Series");
            builder.AddAttribute(6, nameof(RadzenLineSeries<DataItem>.Data), (IEnumerable<DataItem>)SampleData);
            builder.CloseComponent();
        };

        [Theory]
        [InlineData(LegendPosition.Start, "rz-legend-left")]
        [InlineData(LegendPosition.End, "rz-legend-right")]
        public async Task StartEndLegend_InLtr_ResolvesToPhysicalSide(LegendPosition position, string expectedClass)
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p.Add(c => c.ChildContent, LegendWithSeries(position)));
            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));

            Assert.Contains(expectedClass, chart.Markup);
        }

        [Theory]
        [InlineData(LegendPosition.Start, "rz-legend-right")]
        [InlineData(LegendPosition.End, "rz-legend-left")]
        public async Task StartEndLegend_InRtl_FlipsToOppositeSide(LegendPosition position, string expectedClass)
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p.Add(c => c.ChildContent, LegendWithSeries(position)));
            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));
            await chart.InvokeAsync(() => chart.Instance.SetRTL(true));

            Assert.Contains(expectedClass, chart.Markup);
        }

        private static RenderFragment DefaultLegendWithSeries() => builder =>
        {
            builder.OpenComponent<RadzenLegend>(0);
            builder.CloseComponent();

            builder.OpenComponent<RadzenLineSeries<DataItem>>(1);
            builder.AddAttribute(2, nameof(RadzenLineSeries<DataItem>.CategoryProperty), nameof(DataItem.Category));
            builder.AddAttribute(3, nameof(RadzenLineSeries<DataItem>.ValueProperty), nameof(DataItem.Value));
            builder.AddAttribute(4, nameof(RadzenLineSeries<DataItem>.Title), "Series");
            builder.AddAttribute(5, nameof(RadzenLineSeries<DataItem>.Data), (IEnumerable<DataItem>)SampleData);
            builder.CloseComponent();
        };

        [Fact]
        public async Task DefaultLegend_RendersOnRightInLtr_AndFlipsToLeftInRtl()
        {
            using var ctx = CreateChartContext();

            // No Position set -> defaults to LegendPosition.End.
            var chart = ctx.RenderComponent<RadzenChart>(p => p.Add(c => c.ChildContent, DefaultLegendWithSeries()));
            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));

            Assert.Contains("rz-legend-right", chart.Markup);

            await chart.InvokeAsync(() => chart.Instance.SetRTL(true));

            Assert.Contains("rz-legend-left", chart.Markup);
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
