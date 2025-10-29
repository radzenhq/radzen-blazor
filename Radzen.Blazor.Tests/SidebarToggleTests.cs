using Bunit;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class SidebarToggleTests
    {
        [Fact]
        public void SidebarToggle_Renders_WithClassName()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenSidebarToggle>();

            Assert.Contains(@"rz-sidebar-toggle", component.Markup);
        }

        [Fact]
        public void SidebarToggle_Renders_DefaultIcon()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenSidebarToggle>();

            Assert.Contains("menu", component.Markup);
        }

        [Fact]
        public void SidebarToggle_Renders_CustomIcon()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenSidebarToggle>();

            var icon = "close";
            component.SetParametersAndRender(parameters => parameters.Add(p => p.Icon, icon));

            Assert.Contains(icon, component.Markup);
        }

        [Fact]
        public void SidebarToggle_Renders_AriaLabel()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenSidebarToggle>();

            var ariaLabel = "Toggle Navigation";
            component.SetParametersAndRender(parameters => parameters.Add(p => p.ToggleAriaLabel, ariaLabel));

            Assert.Contains($"aria-label=\"{ariaLabel}\"", component.Markup);
        }

        [Fact]
        public void SidebarToggle_Raises_ClickEvent()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenSidebarToggle>();

            var clicked = false;
            component.SetParametersAndRender(parameters => parameters.Add(p => p.Click, args => { clicked = true; }));

            component.Find("button").Click();

            Assert.True(clicked);
        }
    }
}

