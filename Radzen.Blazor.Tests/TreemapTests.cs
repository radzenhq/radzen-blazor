using System.Linq;
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

        [Fact]
        public void Treemap_Visible_Toggle_Recreates_Resizable()
        {
            using var ctx = CreateChartContext();

            var component = ctx.RenderComponent<RadzenTreemap>(parameters =>
            {
                parameters.Add(p => p.Visible, false);
            });

            Assert.Equal(string.Empty, component.Markup.Trim());
            Assert.Empty(ctx.JSInterop.Invocations.Where(i => i.Identifier == "Radzen.createResizable"));

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Visible, true));

            Assert.Contains("rz-treemap", component.Markup);
            Assert.Single(ctx.JSInterop.Invocations.Where(i => i.Identifier == "Radzen.createResizable"));

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Visible, false));

            Assert.Equal(string.Empty, component.Markup.Trim());
            Assert.Single(ctx.JSInterop.Invocations.Where(i => i.Identifier == "Radzen.disposeElement"));

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Visible, true));

            Assert.Equal(2, ctx.JSInterop.Invocations.Count(i => i.Identifier == "Radzen.createResizable"));
        }

        [Fact]
        public void Treemap_Resize_Is_Invokable_Under_The_Name_createResizable_Calls()
        {
            var attribute = typeof(RadzenTreemap).GetMethod(nameof(RadzenTreemap.Resize))!
                .GetCustomAttributes(typeof(Microsoft.JSInterop.JSInvokableAttribute), false)
                .Cast<Microsoft.JSInterop.JSInvokableAttribute>()
                .Single();

            Assert.Equal("Resize", attribute.Identifier ?? "Resize");
        }
    }
}
