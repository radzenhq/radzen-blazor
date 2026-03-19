using Bunit;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class ContentTests
    {
        [Fact]
        public void Content_Renders_WithClassName()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenContent>();

            Assert.Contains("content", component.Markup);
        }

        [Fact]
        public void Content_Renders_ChildContent()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenContent>(parameters =>
            {
                parameters.AddChildContent("<div>Page Content</div>");
            });

            Assert.Contains("Page Content", component.Markup);
        }

        [Fact]
        public void Content_Renders_ContainerParameter()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenContent>(parameters =>
            {
                parameters.Add(p => p.Container, "main-container");
            });

            Assert.NotNull(component.Instance.Container);
        }

        [Fact]
        public void Content_NotVisible_DoesNotRender()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenContent>(parameters =>
            {
                parameters.Add(p => p.Visible, false);
            });

            Assert.DoesNotContain("content", component.Markup);
        }

        // RadzenContentContainer tests
        [Fact]
        public void ContentContainer_Renders_ChildContent()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenContentContainer>(parameters =>
            {
                parameters.AddChildContent("<div>Container Content</div>");
            });

            Assert.Contains("Container Content", component.Markup);
        }

        [Fact]
        public void ContentContainer_Renders_NameParameter()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenContentContainer>(parameters =>
            {
                parameters.Add(p => p.Name, "sidebar");
            });

            Assert.Equal("sidebar", component.Instance.Name);
        }

        [Fact]
        public void ContentContainer_NotVisible_DoesNotRender()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenContentContainer>(parameters =>
            {
                parameters.Add(p => p.Visible, false);
                parameters.AddChildContent("Should Not Show");
            });

            Assert.DoesNotContain("Should Not Show", component.Markup);
        }
    }
}
