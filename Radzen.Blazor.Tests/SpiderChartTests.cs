using System.Linq;
using Bunit;
using Xunit;
using static Radzen.Blazor.Tests.ChartTestHelper;

namespace Radzen.Blazor.Tests
{
    public class SpiderChartTests
    {
        [Fact]
        public void SpiderChart_Visible_Toggle_Recreates_Resizable()
        {
            using var ctx = CreateChartContext();

            var component = ctx.RenderComponent<RadzenSpiderChart>(parameters =>
            {
                parameters.Add(p => p.Visible, false);
            });

            Assert.Equal(string.Empty, component.Markup.Trim());
            Assert.Empty(ctx.JSInterop.Invocations.Where(i => i.Identifier == "Radzen.createResizable"));

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Visible, true));

            Assert.Contains("rz-spider-chart", component.Markup);
            Assert.Single(ctx.JSInterop.Invocations.Where(i => i.Identifier == "Radzen.createResizable"));

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Visible, false));

            Assert.Equal(string.Empty, component.Markup.Trim());
            Assert.Single(ctx.JSInterop.Invocations.Where(i => i.Identifier == "Radzen.disposeElement"));

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Visible, true));

            Assert.Contains("rz-spider-chart", component.Markup);
            Assert.Equal(2, ctx.JSInterop.Invocations.Count(i => i.Identifier == "Radzen.createResizable"));
        }

        [Fact]
        public void SpiderChart_Renders_WithClassName()
        {
            using var ctx = CreateChartContext();

            var component = ctx.RenderComponent<RadzenSpiderChart>();

            Assert.Contains("rz-spider-chart", component.Markup);
        }

        [Fact]
        public void SpiderChart_DefaultGridShape_IsPolygon()
        {
            using var ctx = CreateChartContext();

            var component = ctx.RenderComponent<RadzenSpiderChart>();

            Assert.Equal(SpiderChartGridShape.Polygon, component.Instance.GridShape);
        }

        [Fact]
        public void SpiderChart_ShowMarkers_DefaultTrue()
        {
            using var ctx = CreateChartContext();

            var component = ctx.RenderComponent<RadzenSpiderChart>();

            Assert.True(component.Instance.ShowMarkers);
        }

        [Fact]
        public void SpiderChart_ShowTooltip_DefaultTrue()
        {
            using var ctx = CreateChartContext();

            var component = ctx.RenderComponent<RadzenSpiderChart>();

            Assert.True(component.Instance.ShowTooltip);
        }

        [Fact]
        public void SpiderChart_DefaultAngles()
        {
            using var ctx = CreateChartContext();

            var component = ctx.RenderComponent<RadzenSpiderChart>();

            Assert.Equal(0, component.Instance.StartAngle);
            Assert.Equal(360, component.Instance.EndAngle);
        }

        [Fact]
        public void SpiderChart_Renders_WrapperClass()
        {
            using var ctx = CreateChartContext();

            var component = ctx.RenderComponent<RadzenSpiderChart>();

            Assert.Contains("rz-spider-chart-wrapper", component.Markup);
        }

        [Fact]
        public void SpiderChart_CustomGridShape()
        {
            using var ctx = CreateChartContext();

            var component = ctx.RenderComponent<RadzenSpiderChart>(parameters =>
            {
                parameters.Add(p => p.GridShape, SpiderChartGridShape.Circular);
            });

            Assert.Equal(SpiderChartGridShape.Circular, component.Instance.GridShape);
        }

        [Fact]
        public void SpiderChart_CustomAngles()
        {
            using var ctx = CreateChartContext();

            var component = ctx.RenderComponent<RadzenSpiderChart>(parameters =>
            {
                parameters.Add(p => p.StartAngle, 90);
                parameters.Add(p => p.EndAngle, 270);
            });

            Assert.Equal(90, component.Instance.StartAngle);
            Assert.Equal(270, component.Instance.EndAngle);
        }
    }
}
