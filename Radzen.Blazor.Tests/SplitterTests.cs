using Bunit;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class SplitterTests
    {
        [Fact]
        public void Splitter_Renders_WithClassName()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenSplitter>();

            Assert.Contains(@"rz-splitter", component.Markup);
        }

        [Fact]
        public void Splitter_Renders_Orientation_Horizontal()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenSplitter>(parameters =>
            {
                parameters.Add(p => p.Orientation, Orientation.Horizontal);
            });

            Assert.Contains("rz-splitter-horizontal", component.Markup);
        }

        [Fact]
        public void Splitter_Renders_Orientation_Vertical()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenSplitter>(parameters =>
            {
                parameters.Add(p => p.Orientation, Orientation.Vertical);
            });

            Assert.Contains("rz-splitter-vertical", component.Markup);
        }
    }
}

