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

        [Fact]
        public void Splitter_DefaultOrientation_IsHorizontal()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenSplitter>();

            Assert.Contains("rz-splitter-horizontal", component.Markup);
        }

        [Fact]
        public void Splitter_Renders_StyleParameter()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenSplitter>(parameters =>
            {
                parameters.Add(p => p.Style, "height:400px");
            });

            Assert.Contains("height:400px", component.Markup);
        }

        [Fact]
        public void Splitter_NotVisible_DoesNotRender()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenSplitter>(parameters =>
            {
                parameters.Add(p => p.Visible, false);
            });

            Assert.DoesNotContain("rz-splitter", component.Markup);
        }

        [Fact]
        public void Splitter_Renders_ChildContent()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenSplitter>(parameters =>
            {
                parameters.AddChildContent("<div>Pane Content</div>");
            });

            Assert.Contains("Pane Content", component.Markup);
        }

        [Fact]
        public void Splitter_Renders_UnmatchedAttributes()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenSplitter>(parameters =>
            {
                parameters.AddUnmatched("data-testid", "my-splitter");
            });

            Assert.Contains(@"data-testid=""my-splitter""", component.Markup);
        }
    }
}

