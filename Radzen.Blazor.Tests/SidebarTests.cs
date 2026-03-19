using Bunit;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class SidebarTests
    {
        [Fact]
        public void Sidebar_Renders_WithClassName()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenSidebar>();

            Assert.Contains("rz-sidebar", component.Markup);
        }

        [Fact]
        public void Sidebar_Renders_ChildContent()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenSidebar>(parameters =>
            {
                parameters.AddChildContent("<nav>Menu Items</nav>");
            });

            Assert.Contains("Menu Items", component.Markup);
        }

        [Fact]
        public void Sidebar_Expanded_ByDefault()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenSidebar>();

            Assert.True(component.Instance.Expanded);
        }

        [Fact]
        public void Sidebar_Renders_CollapsedClass_WhenNotExpanded()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenSidebar>(parameters =>
            {
                parameters.Add(p => p.Expanded, false);
                parameters.Add(p => p.Responsive, false);
            });

            Assert.Contains("rz-sidebar-collapsed", component.Markup);
        }

        [Fact]
        public void Sidebar_Renders_ExpandedClass_WhenExpanded()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenSidebar>(parameters =>
            {
                parameters.Add(p => p.Expanded, true);
                parameters.Add(p => p.Responsive, false);
            });

            Assert.Contains("rz-sidebar-expanded", component.Markup);
        }

        [Fact]
        public void Sidebar_Toggle_ChangesState()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenSidebar>(parameters =>
            {
                parameters.Add(p => p.Responsive, false);
            });

            component.InvokeAsync(() => component.Instance.Toggle());

            Assert.Contains("rz-sidebar-collapsed", component.Markup);
        }

        [Fact]
        public void Sidebar_Renders_FullHeightClass()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenSidebar>(parameters =>
            {
                parameters.Add(p => p.FullHeight, true);
            });

            Assert.Contains("rz-sidebar-fullheight", component.Markup);
        }

        [Fact]
        public void Sidebar_DoesNotRender_FullHeight_ByDefault()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenSidebar>();

            Assert.DoesNotContain("rz-sidebar-fullheight", component.Markup);
        }

        [Fact]
        public void Sidebar_Renders_StartPosition_ByDefault()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenSidebar>();

            Assert.Contains("rz-sidebar-start", component.Markup);
        }

        [Fact]
        public void Sidebar_Renders_RightPosition()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenSidebar>(parameters =>
            {
                parameters.Add(p => p.Position, SidebarPosition.Right);
            });

            Assert.Contains("rz-sidebar-right", component.Markup);
        }

        [Fact]
        public void Sidebar_Renders_EndPosition()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenSidebar>(parameters =>
            {
                parameters.Add(p => p.Position, SidebarPosition.End);
            });

            Assert.Contains("rz-sidebar-end", component.Markup);
        }

        [Fact]
        public void Sidebar_NotVisible_DoesNotRender()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenSidebar>(parameters =>
            {
                parameters.Add(p => p.Visible, false);
            });

            Assert.DoesNotContain("rz-sidebar", component.Markup);
        }

        [Fact]
        public void Sidebar_Renders_DefaultStyle()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenSidebar>();

            Assert.Contains("width:250px", component.Markup);
        }
    }
}
