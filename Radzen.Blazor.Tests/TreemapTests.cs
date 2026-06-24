using System.Collections.Generic;
using Bunit;
using Xunit;
using static Radzen.Blazor.Tests.ChartTestHelper;

namespace Radzen.Blazor.Tests
{
    public class TreemapTests
    {
        [Fact]
        public void Treemap_Renders_WithClassName()
        {
            using var ctx = CreateChartContext();

            var component = ctx.RenderComponent<RadzenTreemap>(parameters =>
            {
                parameters.Add(p => p.Data, new List<object>());
            });

            Assert.Contains("rz-treemap", component.Markup);
        }

        [Fact]
        public void Treemap_DefaultPadding()
        {
            using var ctx = CreateChartContext();

            var component = ctx.RenderComponent<RadzenTreemap>(parameters =>
            {
                parameters.Add(p => p.Data, new List<object>());
            });

            Assert.Equal(2, component.Instance.Padding);
        }

        [Fact]
        public void Treemap_ShowLabels_DefaultTrue()
        {
            using var ctx = CreateChartContext();

            var component = ctx.RenderComponent<RadzenTreemap>(parameters =>
            {
                parameters.Add(p => p.Data, new List<object>());
            });

            Assert.True(component.Instance.ShowLabels);
        }

        [Fact]
        public void Treemap_DefaultColorScheme_IsPalette()
        {
            using var ctx = CreateChartContext();

            var component = ctx.RenderComponent<RadzenTreemap>(parameters =>
            {
                parameters.Add(p => p.Data, new List<object>());
            });

            Assert.Equal(ColorScheme.Palette, component.Instance.ColorScheme);
        }

        [Fact]
        public void Treemap_Renders_DivContainer()
        {
            using var ctx = CreateChartContext();

            var component = ctx.RenderComponent<RadzenTreemap>(parameters =>
            {
                parameters.Add(p => p.Data, new List<object>());
            });

            // Treemap renders as div; SVG content appears only after JS resize callback
            Assert.Contains("rz-treemap", component.Markup);
        }

        [Fact]
        public void Treemap_Renders_ColorSchemeClass()
        {
            using var ctx = CreateChartContext();

            var component = ctx.RenderComponent<RadzenTreemap>(parameters =>
            {
                parameters.Add(p => p.Data, new List<object>());
                parameters.Add(p => p.ColorScheme, ColorScheme.Palette);
            });

            Assert.Contains("rz-scheme-palette", component.Markup);
        }
    }
}
