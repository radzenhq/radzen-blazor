using Bunit;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class TabsTests
    {
        [Fact]
        public void Tabs_Renders_WithClassName()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenTabs>();

            Assert.Contains(@"rz-tabview", component.Markup);
        }

        [Fact]
        public void Tabs_Renders_TabPosition_Top()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenTabs>(parameters =>
            {
                parameters.Add(p => p.TabPosition, TabPosition.Top);
            });

            Assert.Contains("rz-tabview-top", component.Markup);
        }

        [Fact]
        public void Tabs_Renders_TabPosition_Bottom()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenTabs>(parameters =>
            {
                parameters.Add(p => p.TabPosition, TabPosition.Bottom);
            });

            Assert.Contains("rz-tabview-bottom", component.Markup);
        }

        [Fact]
        public void Tabs_Renders_TabPosition_Left()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenTabs>(parameters =>
            {
                parameters.Add(p => p.TabPosition, TabPosition.Left);
            });

            Assert.Contains("rz-tabview-left", component.Markup);
        }

        [Fact]
        public void Tabs_Renders_TabPosition_Right()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenTabs>(parameters =>
            {
                parameters.Add(p => p.TabPosition, TabPosition.Right);
            });

            Assert.Contains("rz-tabview-right", component.Markup);
        }

        [Fact]
        public void Tabs_Renders_TabPosition_TopRight()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenTabs>(parameters =>
            {
                parameters.Add(p => p.TabPosition, TabPosition.TopRight);
            });

            Assert.Contains("rz-tabview-top-right", component.Markup);
        }

        [Fact]
        public void Tabs_Renders_TabPosition_BottomRight()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenTabs>(parameters =>
            {
                parameters.Add(p => p.TabPosition, TabPosition.BottomRight);
            });

            Assert.Contains("rz-tabview-bottom-right", component.Markup);
        }

        [Fact]
        public void Tabs_Renders_TabNav()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenTabs>();

            Assert.Contains("rz-tabview-nav", component.Markup);
        }

        [Fact]
        public void Tabs_Renders_TabPanels()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenTabs>();

            Assert.Contains("rz-tabview-panels", component.Markup);
        }
    }
}

