using Bunit;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class LayoutComponentTests
    {
        // RadzenHeader tests
        [Fact]
        public void Header_Renders_WithClassName()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenHeader>();

            Assert.Contains("rz-header", component.Markup);
        }

        [Fact]
        public void Header_Renders_Fixed_WhenNoLayout()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenHeader>();

            Assert.Contains("fixed", component.Markup);
        }

        [Fact]
        public void Header_Renders_ChildContent()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenHeader>(parameters =>
            {
                parameters.AddChildContent("<nav>Navigation</nav>");
            });

            Assert.Contains("Navigation", component.Markup);
        }

        [Fact]
        public void Header_NotVisible_DoesNotRender()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenHeader>(parameters =>
            {
                parameters.Add(p => p.Visible, false);
            });

            Assert.DoesNotContain("rz-header", component.Markup);
        }

        // RadzenFooter tests
        [Fact]
        public void Footer_Renders_WithClassName()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenFooter>();

            Assert.Contains("rz-footer", component.Markup);
        }

        [Fact]
        public void Footer_Renders_Fixed_WhenNoLayout()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenFooter>();

            Assert.Contains("fixed", component.Markup);
        }

        [Fact]
        public void Footer_Renders_ChildContent()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenFooter>(parameters =>
            {
                parameters.AddChildContent("<p>Copyright 2024</p>");
            });

            Assert.Contains("Copyright 2024", component.Markup);
        }

        [Fact]
        public void Footer_NotVisible_DoesNotRender()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenFooter>(parameters =>
            {
                parameters.Add(p => p.Visible, false);
            });

            Assert.DoesNotContain("rz-footer", component.Markup);
        }

        // RadzenBody tests
        [Fact]
        public void Body_Renders_WithClassName()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenBody>();

            Assert.Contains("rz-body", component.Markup);
        }

        [Fact]
        public void Body_Renders_ChildContent()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenBody>(parameters =>
            {
                parameters.AddChildContent("<main>Page Content</main>");
            });

            Assert.Contains("Page Content", component.Markup);
        }

        [Fact]
        public void Body_NotExpanded_ByDefault()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenBody>();

            Assert.DoesNotContain("rz-body-expanded", component.Markup);
        }

        [Fact]
        public void Body_Renders_ExpandedClass()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenBody>(parameters =>
            {
                parameters.Add(p => p.Expanded, true);
            });

            Assert.Contains("rz-body-expanded", component.Markup);
        }

        [Fact]
        public void Body_Toggle_ChangesExpandedState()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenBody>();

            Assert.DoesNotContain("rz-body-expanded", component.Markup);

            component.InvokeAsync(() => component.Instance.Toggle());

            Assert.Contains("rz-body-expanded", component.Markup);
        }

        [Fact]
        public void Body_Toggle_Twice_CollapsesBack()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenBody>();

            component.InvokeAsync(() => component.Instance.Toggle());
            component.InvokeAsync(() => component.Instance.Toggle());

            Assert.DoesNotContain("rz-body-expanded", component.Markup);
        }

        [Fact]
        public void Body_NotVisible_DoesNotRender()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenBody>(parameters =>
            {
                parameters.Add(p => p.Visible, false);
            });

            Assert.DoesNotContain("rz-body", component.Markup);
        }

        [Fact]
        public void Body_Renders_DefaultStyle()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenBody>();

            Assert.Contains("margin-top: 51px", component.Markup);
        }
    }
}
