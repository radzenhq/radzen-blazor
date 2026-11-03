using System.Linq;
using System.Collections.Generic;
using Bunit;
using Xunit;
using static Radzen.Blazor.Tests.ChartTestHelper;

namespace Radzen.Blazor.Tests
{
    public class HeatmapTests
    {
        [Fact]
        public void Heatmap_Renders_WithClassName()
        {
            using var ctx = CreateChartContext();

            var component = ctx.RenderComponent<RadzenHeatmap>(parameters =>
            {
                parameters.Add(p => p.Data, new List<object>());
            });

            Assert.Contains("rz-heatmap", component.Markup);
        }

        [Fact]
        public void Heatmap_DefaultColors()
        {
            using var ctx = CreateChartContext();

            var component = ctx.RenderComponent<RadzenHeatmap>(parameters =>
            {
                parameters.Add(p => p.Data, new List<object>());
            });

            Assert.Equal("#f0f0f0", component.Instance.MinColor);
            Assert.Equal("#1a9641", component.Instance.MaxColor);
        }

        [Fact]
        public void Heatmap_DefaultCellPadding()
        {
            using var ctx = CreateChartContext();

            var component = ctx.RenderComponent<RadzenHeatmap>(parameters =>
            {
                parameters.Add(p => p.Data, new List<object>());
            });

            Assert.Equal(2, component.Instance.CellPadding);
        }

        [Fact]
        public void Heatmap_CustomColors()
        {
            using var ctx = CreateChartContext();

            var component = ctx.RenderComponent<RadzenHeatmap>(parameters =>
            {
                parameters.Add(p => p.Data, new List<object>());
                parameters.Add(p => p.MinColor, "#FFFFFF");
                parameters.Add(p => p.MaxColor, "#FF0000");
            });

            Assert.Equal("#FFFFFF", component.Instance.MinColor);
            Assert.Equal("#FF0000", component.Instance.MaxColor);
        }

        [Fact]
        public void Heatmap_Renders_DivContainer()
        {
            using var ctx = CreateChartContext();

            var component = ctx.RenderComponent<RadzenHeatmap>(parameters =>
            {
                parameters.Add(p => p.Data, new List<object>());
            });

            Assert.Contains("<div", component.Markup);
        }

        [Fact]
        public void Heatmap_ShowValues_DefaultFalse()
        {
            using var ctx = CreateChartContext();

            var component = ctx.RenderComponent<RadzenHeatmap>(parameters =>
            {
                parameters.Add(p => p.Data, new List<object>());
            });

            Assert.False(component.Instance.ShowValues);
        }

        [Fact]
        public void Heatmap_Visible_Toggle_Recreates_Resizable()
        {
            using var ctx = CreateChartContext();

            var component = ctx.RenderComponent<RadzenHeatmap>(parameters =>
            {
                parameters.Add(p => p.Visible, false);
            });

            Assert.Equal(string.Empty, component.Markup.Trim());
            Assert.Empty(ctx.JSInterop.Invocations.Where(i => i.Identifier == "Radzen.createResizable"));

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Visible, true));

            Assert.Contains("rz-heatmap", component.Markup);
            Assert.Single(ctx.JSInterop.Invocations.Where(i => i.Identifier == "Radzen.createResizable"));

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Visible, false));

            Assert.Equal(string.Empty, component.Markup.Trim());
            Assert.Single(ctx.JSInterop.Invocations.Where(i => i.Identifier == "Radzen.disposeElement"));

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Visible, true));

            Assert.Equal(2, ctx.JSInterop.Invocations.Count(i => i.Identifier == "Radzen.createResizable"));
        }

        [Fact]
        public void Heatmap_Resize_Is_Invokable_Under_The_Name_createResizable_Calls()
        {
            var attribute = typeof(RadzenHeatmap).GetMethod(nameof(RadzenHeatmap.Resize))!
                .GetCustomAttributes(typeof(Microsoft.JSInterop.JSInvokableAttribute), false)
                .Cast<Microsoft.JSInterop.JSInvokableAttribute>()
                .Single();

            Assert.Equal("Resize", attribute.Identifier ?? "Resize");
        }
    }
}
