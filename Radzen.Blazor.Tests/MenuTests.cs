using Bunit;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class MenuTests
    {
        [Fact]
        public void Menu_Renders_WithClassName()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenMenu>();

            Assert.Contains(@"rz-menu", component.Markup);
        }

        [Fact]
        public void Menu_Renders_Responsive_True()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenMenu>(parameters =>
            {
                parameters.Add(p => p.Responsive, true);
            });

            Assert.Contains("rz-menu-closed", component.Markup);
            Assert.Contains("rz-menu-toggle", component.Markup);
        }

        [Fact]
        public void Menu_Renders_Responsive_False()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenMenu>(parameters =>
            {
                parameters.Add(p => p.Responsive, false);
            });

            Assert.DoesNotContain("rz-menu-toggle", component.Markup);
            Assert.DoesNotContain("rz-menu-closed", component.Markup);
        }


        [Fact]
        public void Menu_Renders_CustomToggleAriaLabel()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenMenu>(parameters =>
            {
                parameters.Add(p => p.Responsive, true);
                parameters.Add(p => p.ToggleAriaLabel, "Open navigation");
            });

            Assert.Contains("Open navigation", component.Markup);
        }
    }
}

