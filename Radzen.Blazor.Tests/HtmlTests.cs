using Bunit;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class HtmlTests
    {
        [Fact]
        public void Html_Renders_ChildContent()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenHtml>(parameters =>
            {
                parameters.AddChildContent("<p>Hello World</p>");
            });

            Assert.Contains("<p>Hello World</p>", component.Markup);
        }

        [Fact]
        public void Html_Visible_True_RendersContent()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenHtml>(parameters =>
            {
                parameters.Add(p => p.Visible, true);
                parameters.AddChildContent("<span>Visible</span>");
            });

            Assert.Contains("Visible", component.Markup);
        }

        [Fact]
        public void Html_Visible_False_DoesNotRender()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenHtml>(parameters =>
            {
                parameters.Add(p => p.Visible, false);
                parameters.AddChildContent("<span>Hidden</span>");
            });

            Assert.DoesNotContain("Hidden", component.Markup);
        }

        [Fact]
        public void Html_Renders_HtmlMarkup()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenHtml>(parameters =>
            {
                parameters.AddChildContent("<strong>Bold</strong> and <em>italic</em>");
            });

            Assert.Contains("<strong>Bold</strong>", component.Markup);
            Assert.Contains("<em>italic</em>", component.Markup);
        }

        [Fact]
        public void Html_EmptyContent_Renders()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenHtml>();

            // Should not throw with no child content
            Assert.NotNull(component);
        }
    }
}
