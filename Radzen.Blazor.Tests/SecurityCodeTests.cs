using Bunit;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class SecurityCodeTests
    {
        [Fact]
        public void SecurityCode_Renders_WithClassName()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenSecurityCode>();

            Assert.Contains(@"rz-security-code", component.Markup);
        }

        [Fact]
        public void SecurityCode_Renders_Count()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenSecurityCode>();

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Count, 6));

            // Should render 6 input boxes + 1 hidden input for form submission = 7 total
            var inputCount = System.Text.RegularExpressions.Regex.Matches(component.Markup, "<input").Count;
            Assert.Equal(7, inputCount);
        }

        [Fact]
        public void SecurityCode_Renders_Disabled()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenSecurityCode>();

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Disabled, true));

            Assert.Contains("disabled", component.Markup);
        }

        [Fact]
        public void SecurityCode_DefaultCount_IsFour()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenSecurityCode>();

            // Default count is 4, so 4 inputs + 1 hidden = 5
            var inputCount = System.Text.RegularExpressions.Regex.Matches(component.Markup, "<input").Count;
            Assert.Equal(5, inputCount);
        }

        [Fact]
        public void SecurityCode_NotVisible_DoesNotRender()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenSecurityCode>(parameters =>
            {
                parameters.Add(p => p.Visible, false);
            });

            Assert.DoesNotContain("rz-security-code", component.Markup);
        }

        [Fact]
        public void SecurityCode_Renders_Gap()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenSecurityCode>(parameters =>
            {
                parameters.Add(p => p.Gap, "1rem");
            });

            Assert.Contains("1rem", component.Markup);
        }

        [Fact]
        public void SecurityCode_Renders_StyleParameter()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenSecurityCode>(parameters =>
            {
                parameters.Add(p => p.Style, "margin:2rem");
            });

            Assert.Contains("margin:2rem", component.Markup);
        }
    }
}

