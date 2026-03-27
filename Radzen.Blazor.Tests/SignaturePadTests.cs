using Bunit;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class SignaturePadTests
    {
        [Fact]
        public void SignaturePad_Renders_WithClassName()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenSignaturePad>();

            Assert.Contains("rz-signature-pad", component.Markup);
        }

        [Fact]
        public void SignaturePad_Renders_Canvas()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenSignaturePad>();

            Assert.Contains("<canvas", component.Markup);
            Assert.Contains("rz-signature-pad-canvas", component.Markup);
        }

        [Fact]
        public void SignaturePad_Renders_Disabled()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenSignaturePad>(parameters =>
            {
                parameters.Add(p => p.Disabled, true);
            });

            Assert.Contains("rz-state-disabled", component.Markup);
        }

        [Fact]
        public void SignaturePad_Renders_ReadOnly()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenSignaturePad>(parameters =>
            {
                parameters.Add(p => p.ReadOnly, true);
            });

            Assert.Contains("rz-state-readonly", component.Markup);
        }

        [Fact]
        public void SignaturePad_NotVisible_DoesNotRender()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenSignaturePad>(parameters =>
            {
                parameters.Add(p => p.Visible, false);
            });

            Assert.DoesNotContain("rz-signature-pad", component.Markup);
        }

        [Fact]
        public void SignaturePad_Renders_StyleParameter()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenSignaturePad>(parameters =>
            {
                parameters.Add(p => p.Style, "margin:2rem");
            });

            Assert.Contains("margin:2rem", component.Markup);
        }


        [Fact]
        public void SignaturePad_Renders_Placeholder_WhenEmpty()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenSignaturePad>(parameters =>
            {
                parameters.Add(p => p.Placeholder, "Sign here");
            });

            Assert.Contains("Sign here", component.Markup);
            Assert.Contains("rz-signature-pad-placeholder", component.Markup);
        }

        [Fact]
        public void SignaturePad_DoesNotRender_Placeholder_WhenHasValue()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenSignaturePad>(parameters =>
            {
                parameters.Add(p => p.Placeholder, "Sign here");
                parameters.Add(p => p.Value, "data:image/png;base64,test");
            });

            Assert.DoesNotContain("rz-signature-pad-placeholder", component.Markup);
        }

        [Fact]
        public void SignaturePad_Renders_ClearButton_WhenHasValue()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenSignaturePad>(parameters =>
            {
                parameters.Add(p => p.Value, "data:image/png;base64,test");
            });

            Assert.Contains("rz-signature-pad-clear", component.Markup);
        }

        [Fact]
        public void SignaturePad_DoesNotRender_ClearButton_WhenEmpty()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenSignaturePad>();

            Assert.DoesNotContain("rz-signature-pad-clear", component.Markup);
        }

        [Fact]
        public void SignaturePad_DoesNotRender_ClearButton_WhenAllowClearFalse()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenSignaturePad>(parameters =>
            {
                parameters.Add(p => p.Value, "data:image/png;base64,test");
                parameters.Add(p => p.AllowClear, false);
            });

            Assert.DoesNotContain("rz-signature-pad-clear", component.Markup);
        }

        [Fact]
        public void SignaturePad_DoesNotRender_ClearButton_WhenDisabled()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenSignaturePad>(parameters =>
            {
                parameters.Add(p => p.Value, "data:image/png;base64,test");
                parameters.Add(p => p.Disabled, true);
            });

            Assert.DoesNotContain("rz-signature-pad-clear", component.Markup);
        }

        [Fact]
        public void SignaturePad_Renders_ClearButton_WhenReadOnly()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenSignaturePad>(parameters =>
            {
                parameters.Add(p => p.Value, "data:image/png;base64,test");
                parameters.Add(p => p.ReadOnly, true);
            });

            Assert.Contains("rz-signature-pad-clear", component.Markup);
        }

        [Fact]
        public void SignaturePad_ClearButton_HasAriaLabel()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenSignaturePad>(parameters =>
            {
                parameters.Add(p => p.Value, "data:image/png;base64,test");
                parameters.Add(p => p.ClearAriaLabel, "Remove signature");
            });

            Assert.Contains("aria-label=\"Remove signature\"", component.Markup);
        }

    }
}
