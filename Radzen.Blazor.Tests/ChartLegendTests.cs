using System.Collections.Generic;
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

        [Fact]
        public async Task LegendItem_LineSwatch_UsesCustomSeriesStroke()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .AddChildContent<RadzenLegend>()
                .AddChildContent<RadzenLineSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Stroke, "#6366f1")
                    .Add(x => x.Data, SampleData)));

            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));

            var line = chart.Find("line.rz-legend-item-line");
            Assert.Equal("#6366f1", line.GetAttribute("stroke"));
        }

        private static RenderFragment<LegendItemContext> ItemTemplate => item => builder =>
        {
            builder.OpenElement(0, "em");
            builder.AddContent(1, item.Data is DataItem data ? $"{item.Text}: {data.Value}" : $"{item.Text}!");
            builder.CloseElement();
        };

        private static RenderFragment TemplatedLegend(LegendPosition position, bool template, bool pie) => builder =>
        {
            builder.OpenComponent<RadzenLegend>(0);
            builder.AddAttribute(1, nameof(RadzenLegend.Position), position);

            if (template)
            {
                builder.AddAttribute(2, nameof(RadzenLegend.ItemTemplate), ItemTemplate);
            }

            builder.CloseComponent();

            if (pie)
            {
                builder.OpenComponent<RadzenPieSeries<DataItem>>(3);
            }
            else
            {
                builder.OpenComponent<RadzenLineSeries<DataItem>>(3);
            }

            builder.AddAttribute(4, nameof(RadzenLineSeries<DataItem>.CategoryProperty), nameof(DataItem.Category));
            builder.AddAttribute(5, nameof(RadzenLineSeries<DataItem>.ValueProperty), nameof(DataItem.Value));
            builder.AddAttribute(6, nameof(RadzenLineSeries<DataItem>.Title), "Series");
            builder.AddAttribute(7, nameof(RadzenLineSeries<DataItem>.Data), (IEnumerable<DataItem>)SampleData);
            builder.CloseComponent();
        };

        [Fact]
        public async Task ItemTemplate_RendersInsteadOfText_AndKeepsMarker()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p.Add(c => c.ChildContent, TemplatedLegend(LegendPosition.Right, true, false)));
            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));

            var item = chart.Find(".rz-legend-item");

            Assert.Equal("<em>Series!</em>", item.QuerySelector(".rz-legend-item-text").InnerHtml);
            Assert.NotNull(item.QuerySelector("svg"));
        }

        [Fact]
        public async Task ItemTemplate_ReceivesDataItem_ForPieSeries()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p.Add(c => c.ChildContent, TemplatedLegend(LegendPosition.Right, true, true)));
            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));

            var texts = chart.FindAll(".rz-legend-item-text").Select(e => e.InnerHtml).ToArray();

            Assert.Equal(new[] { "<em>A: 10</em>", "<em>B: 20</em>", "<em>C: 15</em>" }, texts);
        }

        [Fact]
        public async Task ItemTemplate_RequestsLegendMeasuring()
        {
            using var ctx = CreateChartContext();

            var templated = ctx.RenderComponent<RadzenChart>(p => p.Add(c => c.ChildContent, TemplatedLegend(LegendPosition.Top, true, false)));
            await templated.InvokeAsync(() => templated.Instance.Resize(400, 300));

            var invocation = Assert.Single(ctx.JSInterop.Invocations, i => i.Identifier == "Radzen.observeChartLegend");
            Assert.Equal(true, invocation.Arguments[1]);
        }

        [Fact]
        public async Task LegendWithoutItemTemplate_DoesNotRequestLegendMeasuring()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p.Add(c => c.ChildContent, TemplatedLegend(LegendPosition.Top, false, false)));
            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));

            Assert.DoesNotContain(ctx.JSInterop.Invocations, i => i.Identifier == "Radzen.observeChartLegend");
        }

        [Fact]
        public async Task ItemTemplate_ReservesTheMeasuredLegendSize()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p.Add(c => c.ChildContent, TemplatedLegend(LegendPosition.Top, true, false)));
            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));

            var estimated = MarginTopOf(chart.Markup);

            await chart.InvokeAsync(() => chart.Instance.LegendResize(90, false));

            Assert.NotEqual(90, estimated);
            Assert.Equal(90, MarginTopOf(chart.Markup));
        }

        [Fact]
        public async Task MeasuredLegendSize_IsIgnored_ForTheOtherOrientation_AndWithoutItemTemplate()
        {
            using var ctx = CreateChartContext();

            var templated = ctx.RenderComponent<RadzenChart>(p => p.Add(c => c.ChildContent, TemplatedLegend(LegendPosition.Top, true, false)));
            await templated.InvokeAsync(() => templated.Instance.Resize(400, 300));
            var estimated = MarginTopOf(templated.Markup);
            await templated.InvokeAsync(() => templated.Instance.LegendResize(90, true));

            Assert.Equal(estimated, MarginTopOf(templated.Markup));

            var plain = ctx.RenderComponent<RadzenChart>(p => p.Add(c => c.ChildContent, TemplatedLegend(LegendPosition.Top, false, false)));
            await plain.InvokeAsync(() => plain.Instance.Resize(400, 300));
            await plain.InvokeAsync(() => plain.Instance.LegendResize(90, false));

            Assert.Equal(estimated, MarginTopOf(plain.Markup));
        }
    }
}
