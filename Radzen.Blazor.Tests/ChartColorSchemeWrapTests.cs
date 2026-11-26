using System.Linq;
using Bunit;
using Microsoft.AspNetCore.Components;
using Xunit;
using static Radzen.Blazor.Tests.ChartTestHelper;

namespace Radzen.Blazor.Tests
{
    public class ChartColorSchemeWrapTests
    {
        static RenderFragment LineSeries(int count) => builder =>
        {
            for (var i = 0; i < count; i++)
            {
                builder.OpenComponent<RadzenLineSeries<DataItem>>(0);
                builder.SetKey(i);
                builder.AddAttribute(1, nameof(RadzenLineSeries<DataItem>.CategoryProperty), nameof(DataItem.Category));
                builder.AddAttribute(2, nameof(RadzenLineSeries<DataItem>.ValueProperty), nameof(DataItem.Value));
                builder.AddAttribute(3, nameof(RadzenLineSeries<DataItem>.Data), SampleData);
                builder.CloseComponent();
            }
        };

        static DataItem[] Items(int count) => Enumerable.Range(0, count)
            .Select(i => new DataItem { Category = $"Item {i}", Value = i + 1 })
            .ToArray();

        [Theory]
        [InlineData(ColorScheme.Pastel, 24)]
        [InlineData(ColorScheme.Palette, 8)]
        [InlineData(ColorScheme.Monochrome, 8)]
        [InlineData(ColorScheme.Divergent, 9)]
        public void Series_BeyondSchemeLength_UseSchemeColorShades(ColorScheme scheme, int length)
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .Add(x => x.ColorScheme, scheme)
                .Add(x => x.ChildContent, LineSeries(length + 2)));

            Assert.Contains($"rz-line-series rz-series-{length - 1}\"", chart.Markup);
            Assert.Contains($"rz-line-series rz-series-{length} rz-series-color-0 rz-series-shade-1\"", chart.Markup);
            Assert.Contains($"rz-line-series rz-series-{length + 1} rz-series-color-1 rz-series-shade-1\"", chart.Markup);
        }

        [Fact]
        public void Series_WithinSchemeLength_HaveNoColorClass()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .Add(x => x.ChildContent, LineSeries(24)));

            Assert.DoesNotContain("rz-series-color-", chart.Markup);
        }

        [Fact]
        public void LegendItem_BeyondSchemeLength_UsesSchemeColorShade()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .Add(x => x.ColorScheme, ColorScheme.Palette)
                .Add(x => x.ChildContent, LineSeries(9)));

            var marker = chart.FindAll(".rz-legend-item g").Last(g => g.ClassList.Contains("rz-series-8"));

            Assert.Contains("rz-series-color-0", marker.ClassList);
            Assert.Contains("rz-series-shade-1", marker.ClassList);
        }

        [Fact]
        public void PieSeries_ItemsBeyondSchemeLength_UseSchemeColorShades()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .Add(x => x.ColorScheme, ColorScheme.Palette)
                .AddChildContent<RadzenPieSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Data, Items(10))));

            var slices = chart.FindAll(".rz-pie-series > g");

            Assert.Equal("rz-series-item-7", slices[7].GetAttribute("class"));
            Assert.Equal("rz-series-item-8 rz-series-color-0 rz-series-shade-1", slices[8].GetAttribute("class"));
            Assert.Equal("rz-series-item-9 rz-series-color-1 rz-series-shade-1", slices[9].GetAttribute("class"));
        }

        [Fact]
        public void BulletSeries_BeyondSchemeLength_UsesSchemeColorShades()
        {
            using var ctx = CreateChartContext();

            RenderFragment series = builder =>
            {
                for (var i = 0; i < 26; i++)
                {
                    builder.OpenComponent<RadzenBulletSeries<DataItem>>(0);
                    builder.SetKey(i);
                    builder.AddAttribute(1, nameof(RadzenBulletSeries<DataItem>.CategoryProperty), nameof(DataItem.Category));
                    builder.AddAttribute(2, nameof(RadzenBulletSeries<DataItem>.ValueProperty), nameof(DataItem.Value));
                    builder.AddAttribute(3, nameof(RadzenBulletSeries<DataItem>.TargetProperty), nameof(DataItem.Target));
                    builder.AddAttribute(4, nameof(RadzenBulletSeries<DataItem>.MaxProperty), nameof(DataItem.Max));
                    builder.AddAttribute(5, nameof(RadzenBulletSeries<DataItem>.Data), new[] { new DataItem { Category = "Revenue", Value = 270, Target = 300, Max = 400 } });
                    builder.CloseComponent();
                }
            };

            var chart = ctx.RenderComponent<RadzenChart>(p => p.Add(x => x.ChildContent, series));

            Assert.Contains("rz-bullet-series rz-series-25 rz-series-color-1 rz-series-shade-1\"", chart.Markup);
            Assert.Contains("var(--rz-series-24)", chart.Markup);
            Assert.Contains("color-mix(in oklch, var(--rz-series-2), var(--rz-series-shade-color, white) var(--rz-series-shade, 0%))", chart.Markup);
            Assert.DoesNotContain("var(--rz-series-25)", chart.Markup);
            Assert.DoesNotContain("var(--rz-series-26)", chart.Markup);
        }

        [Theory]
        [InlineData(ColorScheme.Pastel, 23, "rz-series-23-tooltip")]
        [InlineData(ColorScheme.Pastel, 24, "rz-series-24-tooltip rz-series-color-0-tooltip rz-series-shade-1")]
        [InlineData(ColorScheme.Palette, 9, "rz-series-9-tooltip rz-series-color-1-tooltip rz-series-shade-1")]
        [InlineData(ColorScheme.Divergent, 8, "rz-series-8-tooltip")]
        public void SeriesTooltipClass_KeepsIndexAndAddsSchemeColor(ColorScheme scheme, int index, string expected)
        {
            Assert.Equal(expected, scheme.SeriesTooltipClass(index));
        }

        [Theory]
        [InlineData(ColorScheme.Palette, 7, "rz-series-item-7")]
        [InlineData(ColorScheme.Palette, 8, "rz-series-item-8 rz-series-color-0-tooltip rz-series-shade-1")]
        [InlineData(ColorScheme.Pastel, -1, "rz-series-item--1")]
        public void SeriesItemTooltipClass_KeepsIndexAndAddsSchemeColor(ColorScheme scheme, int index, string expected)
        {
            Assert.Equal(expected, scheme.SeriesItemTooltipClass(index));
        }

        [Theory]
        [InlineData(ColorScheme.Palette, 7, null)]
        [InlineData(ColorScheme.Palette, 8, "rz-series-color-0 rz-series-shade-1")]
        [InlineData(ColorScheme.Palette, 17, "rz-series-color-1 rz-series-shade-2")]
        [InlineData(ColorScheme.Palette, 24, "rz-series-color-0 rz-series-shade-3")]
        [InlineData(ColorScheme.Palette, 32, "rz-series-color-0 rz-series-shade-1")]
        [InlineData(ColorScheme.Pastel, 47, "rz-series-color-23 rz-series-shade-1")]
        [InlineData(ColorScheme.Pastel, 48, "rz-series-color-0 rz-series-shade-2")]
        public void ColorClass_ChangesShadeOnEveryPassThroughTheScheme(ColorScheme scheme, int index, string expected)
        {
            Assert.Equal(expected, scheme.ColorClass(index));
        }

        [Theory]
        [InlineData(23, "rz-series-item-23")]
        [InlineData(24, "rz-series-item-24 rz-series-item-0")]
        [InlineData(50, "rz-series-item-50 rz-series-item-2")]
        [InlineData(-1, "rz-series-item--1")]
        public void SankeyItemClass_KeepsIndexAndAddsSchemeColor(int index, string expected)
        {
            Assert.Equal(expected, ChartColorClasses.SankeyItemClass(index));
        }
    }
}
