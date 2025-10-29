using Bunit;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class TimelineTests
    {
        [Fact]
        public void Timeline_Renders_WithClassName()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenTimeline>();

            Assert.Contains(@"rz-timeline", component.Markup);
        }

        [Fact]
        public void Timeline_Renders_Orientation()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenTimeline>();

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Orientation, Orientation.Horizontal));
            Assert.Contains("rz-timeline-row", component.Markup);

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Orientation, Orientation.Vertical));
            Assert.Contains("rz-timeline-column", component.Markup);
        }

        [Fact]
        public void Timeline_Renders_LinePosition()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenTimeline>();

            component.SetParametersAndRender(parameters => parameters.Add(p => p.LinePosition, LinePosition.Start));
            Assert.Contains("rz-timeline-start", component.Markup);

            component.SetParametersAndRender(parameters => parameters.Add(p => p.LinePosition, LinePosition.End));
            Assert.Contains("rz-timeline-end", component.Markup);

            component.SetParametersAndRender(parameters => parameters.Add(p => p.LinePosition, LinePosition.Center));
            Assert.Contains("rz-timeline-center", component.Markup);
        }

        [Fact]
        public void Timeline_Renders_AlignItems()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenTimeline>();

            component.SetParametersAndRender(parameters => parameters.Add(p => p.AlignItems, AlignItems.Start));
            Assert.Contains("rz-timeline-align-items-start", component.Markup);

            component.SetParametersAndRender(parameters => parameters.Add(p => p.AlignItems, AlignItems.Center));
            Assert.Contains("rz-timeline-align-items-center", component.Markup);
        }

        [Fact]
        public void Timeline_Renders_Reverse()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenTimeline>();

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Reverse, true));

            Assert.Contains("rz-timeline-reverse", component.Markup);
        }
    }
}

