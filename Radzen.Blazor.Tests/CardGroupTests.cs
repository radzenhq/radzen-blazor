using Bunit;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class CardGroupTests
    {
        [Fact]
        public void CardGroup_Renders_WithClassName()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenCardGroup>();

            Assert.Contains("rz-card-group", component.Markup);
        }

        [Fact]
        public void CardGroup_Renders_Responsive_ByDefault()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenCardGroup>();

            Assert.Contains("rz-card-group-responsive", component.Markup);
        }

        [Fact]
        public void CardGroup_Renders_NonResponsive()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenCardGroup>(parameters =>
            {
                parameters.Add(p => p.Responsive, false);
            });

            Assert.DoesNotContain("rz-card-group-responsive", component.Markup);
        }

        [Fact]
        public void CardGroup_Renders_ChildContent()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenCardGroup>(parameters =>
            {
                parameters.AddChildContent("<div>Card 1</div><div>Card 2</div>");
            });

            Assert.Contains("Card 1", component.Markup);
            Assert.Contains("Card 2", component.Markup);
        }

        [Fact]
        public void CardGroup_NotVisible_DoesNotRender()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenCardGroup>(parameters =>
            {
                parameters.Add(p => p.Visible, false);
            });

            Assert.DoesNotContain("rz-card-group", component.Markup);
        }

        [Fact]
        public void CardGroup_Renders_StyleParameter()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenCardGroup>(parameters =>
            {
                parameters.Add(p => p.Style, "gap:1rem");
            });

            Assert.Contains("gap:1rem", component.Markup);
        }
    }
}
