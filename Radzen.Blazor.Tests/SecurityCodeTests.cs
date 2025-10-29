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
    }
}

