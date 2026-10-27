using Bunit;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class SidebarToggleTests
    {
        [Fact]
        public void SidebarToggle_Renders_WithClassName()
        {
            using var ctx = new BunitContext();
            var component = ctx.Render<RadzenSidebarToggle>();

            Assert.Contains(@"rz-sidebar-toggle", component.Markup);
        }

        [Fact]
        public void SidebarToggle_Renders_DefaultIcon()
        {
            using var ctx = new BunitContext();
            var component = ctx.Render<RadzenSidebarToggle>();

            Assert.Contains("menu", component.Markup);
        }

        [Fact]
        public void SidebarToggle_Renders_CustomIcon()
        {
            using var ctx = new BunitContext();
            var component = ctx.Render<RadzenSidebarToggle>();

            var icon = "close";
            component.Render(parameters => parameters.Add(p => p.Icon, icon));

            Assert.Contains(icon, component.Markup);
        }

        [Fact]
        public void SidebarToggle_Renders_AriaLabel()
        {
            using var ctx = new BunitContext();
            var component = ctx.Render<RadzenSidebarToggle>();

            var ariaLabel = "Toggle Navigation";
            component.Render(parameters => parameters.Add(p => p.ToggleAriaLabel, ariaLabel));

            Assert.Contains($"aria-label=\"{ariaLabel}\"", component.Markup);
        }

        [Fact]
        public void SidebarToggle_Raises_ClickEvent()
        {
            using var ctx = new BunitContext();
            var component = ctx.Render<RadzenSidebarToggle>();

            var clicked = false;
            component.Render(parameters => parameters.Add(p => p.Click, args => { clicked = true; }));

            component.Find("button").Click();

            Assert.True(clicked);
        }
    }
}

