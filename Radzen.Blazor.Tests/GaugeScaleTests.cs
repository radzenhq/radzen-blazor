using Bunit;
using Radzen.Blazor.Rendering;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class GaugeScaleTests
    {
        static IRenderedComponent<GaugeScale> RenderScale(TestContext ctx, bool showTickLabels) =>
            ctx.RenderComponent<GaugeScale>(parameters => parameters
                .Add(p => p.StartAngle, -90)
                .Add(p => p.EndAngle, 90)
                .Add(p => p.Radius, 100)
                .Add(p => p.Center, new Point { X = 150, Y = 150 })
                .Add(p => p.Min, 0)
                .Add(p => p.Max, 100)
                .Add(p => p.Step, 20)
                .Add(p => p.ShowFirstTick, true)
                .Add(p => p.ShowLastTick, true)
                .Add(p => p.ShowTickLabels, showTickLabels)
                .Add(p => p.TickPosition, GaugeTickPosition.Outside)
                .Add(p => p.TickLength, 5)
                .Add(p => p.TickLabelOffset, 15));

        [Fact]
        public void GaugeScale_Renders_TickLabels_WithoutLeakedText()
        {
            using var ctx = new TestContext();

            var component = RenderScale(ctx, true);

            Assert.DoesNotContain("IsNullOrEmpty", component.Markup);
            Assert.Equal(6, component.FindAll("g.rz-tick text.rz-tick-text").Count);
        }

        [Fact]
        public void GaugeScale_DoesNotRender_TickText_WhenLabelsHidden()
        {
            using var ctx = new TestContext();

            var component = RenderScale(ctx, false);

            Assert.DoesNotContain("IsNullOrEmpty", component.Markup);
            Assert.Equal(6, component.FindAll("g.rz-tick").Count);
            Assert.Empty(component.FindAll("text"));
        }
    }
}
