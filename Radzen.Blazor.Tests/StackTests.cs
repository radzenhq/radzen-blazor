using Bunit;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class StackTests
    {
        [Fact]
        public void Stack_Renders_WithClassName()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenStack>();

            Assert.Contains(@"rz-stack", component.Markup);
        }

        [Fact]
        public void Stack_Renders_ChildContent()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenStack>(parameters =>
            {
                parameters.AddChildContent("<div>Stack Content</div>");
            });

            Assert.Contains("Stack Content", component.Markup);
        }

        [Fact]
        public void Stack_Renders_Orientation()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenStack>();

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Orientation, Orientation.Horizontal));
            Assert.Contains("rz-flex-row", component.Markup);

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Orientation, Orientation.Vertical));
            Assert.Contains("rz-flex-column", component.Markup);
        }

        [Fact]
        public void Stack_Renders_Gap()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenStack>();

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Gap, "1.5rem"));

            Assert.Contains("--rz-gap:1.5rem", component.Markup);
        }

        [Fact]
        public void Stack_Renders_AlignItems()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenStack>();

            component.SetParametersAndRender(parameters => parameters.Add(p => p.AlignItems, AlignItems.Center));

            Assert.Contains("rz-align-items-center", component.Markup);
        }

        [Fact]
        public void Stack_Renders_JustifyContent()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenStack>();

            component.SetParametersAndRender(parameters => parameters.Add(p => p.JustifyContent, JustifyContent.SpaceBetween));

            Assert.Contains("rz-justify-content-space-between", component.Markup);
        }

        [Fact]
        public void Stack_Renders_Wrap()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenStack>();

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Wrap, FlexWrap.Wrap));

            Assert.Contains("flex-wrap:wrap", component.Markup);
        }

        [Fact]
        public void Stack_Renders_Reverse()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenStack>();

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Reverse, true));

            Assert.Contains("rz-flex-column-reverse", component.Markup);
        }
    }
}

