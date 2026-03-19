using Bunit;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class QRCodeTests
    {
        [Fact]
        public void QRCode_Renders_WithClassName()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenQRCode>(parameters =>
            {
                parameters.Add(p => p.Value, "https://example.com");
            });

            Assert.Contains("rz-qrcode", component.Markup);
        }

        [Fact]
        public void QRCode_Renders_SvgWithModules()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenQRCode>(parameters =>
            {
                parameters.Add(p => p.Value, "Hello");
            });

            // QR code renders rect elements for each module and has a viewBox
            Assert.Contains("<rect", component.Markup);
            Assert.Contains("viewBox=", component.Markup);
            Assert.Contains(@"width=""100%""", component.Markup);
        }

        [Fact]
        public void QRCode_DefaultSize_Is100Percent()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenQRCode>();

            Assert.Equal("100%", component.Instance.Size);
        }

        [Fact]
        public void QRCode_DefaultColors()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenQRCode>();

            Assert.Equal("#000", component.Instance.Foreground);
            Assert.Equal("#FFF", component.Instance.Background);
        }

        [Fact]
        public void QRCode_DefaultModuleShape_IsSquare()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenQRCode>();

            Assert.Equal(QRCodeModuleShape.Square, component.Instance.ModuleShape);
        }

        [Fact]
        public void QRCode_DefaultEyeShape_IsSquare()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenQRCode>();

            Assert.Equal(QRCodeEyeShape.Square, component.Instance.EyeShape);
        }

        [Fact]
        public void QRCode_DefaultEcc_IsQuartile()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenQRCode>();

            Assert.Equal(RadzenQREcc.Quartile, component.Instance.Ecc);
        }

        [Fact]
        public void QRCode_Renders_CustomForeground()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenQRCode>(parameters =>
            {
                parameters.Add(p => p.Value, "Test");
                parameters.Add(p => p.Foreground, "#FF0000");
            });

            Assert.Contains("#FF0000", component.Markup);
        }

        [Fact]
        public void QRCode_Renders_CustomBackground()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenQRCode>(parameters =>
            {
                parameters.Add(p => p.Value, "Test");
                parameters.Add(p => p.Background, "#00FF00");
            });

            // Background gets converted to rgb format in SVG
            Assert.Equal("#00FF00", component.Instance.Background);
        }

        [Fact]
        public void QRCode_Renders_RoundedModuleShape()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenQRCode>(parameters =>
            {
                parameters.Add(p => p.Value, "Test");
                parameters.Add(p => p.ModuleShape, QRCodeModuleShape.Rounded);
            });

            // Rounded modules use rx attribute on rect elements for rounded corners
            Assert.Contains("<rect", component.Markup);
            Assert.Equal(QRCodeModuleShape.Rounded, component.Instance.ModuleShape);
        }

        [Fact]
        public void QRCode_Renders_CircleModuleShape()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenQRCode>(parameters =>
            {
                parameters.Add(p => p.Value, "Test");
                parameters.Add(p => p.ModuleShape, QRCodeModuleShape.Circle);
            });

            // Circle modules use <circle> elements instead of <rect>
            Assert.Contains("<circle", component.Markup);
            Assert.Equal(QRCodeModuleShape.Circle, component.Instance.ModuleShape);
        }

        [Fact]
        public void QRCode_Renders_CustomEyeShape()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenQRCode>(parameters =>
            {
                parameters.Add(p => p.Value, "Test");
                parameters.Add(p => p.EyeShape, QRCodeEyeShape.Rounded);
            });

            Assert.Equal(QRCodeEyeShape.Rounded, component.Instance.EyeShape);
        }

        [Fact]
        public void QRCode_Renders_WithImage()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenQRCode>(parameters =>
            {
                parameters.Add(p => p.Value, "Test");
                parameters.Add(p => p.Image, "logo.png");
                parameters.Add(p => p.Ecc, RadzenQREcc.High);
            });

            Assert.Contains("logo.png", component.Markup);
        }

        [Fact]
        public void QRCode_DifferentValues_ProduceDifferentOutput()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component1 = ctx.RenderComponent<RadzenQRCode>(parameters =>
            {
                parameters.Add(p => p.Value, "Hello");
            });

            var component2 = ctx.RenderComponent<RadzenQRCode>(parameters =>
            {
                parameters.Add(p => p.Value, "World");
            });

            Assert.NotEqual(component1.Markup, component2.Markup);
        }
    }
}
