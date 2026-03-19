using Bunit;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class QRCodeTests
    {
        [Fact]
        public void QRCode_AB_Renders_CorrectViewBox()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenQRCode>(p =>
            {
                p.Add(x => x.Value, "AB");
            });

            // QR code for "AB" with Quartile ECC: 21x21 modules + 2*4 quiet zone = 29x29
            Assert.Contains(@"viewBox=""0 0 29 29""", component.Markup);
        }

        [Fact]
        public void QRCode_AB_Renders_BackgroundRect()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenQRCode>(p =>
            {
                p.Add(x => x.Value, "AB");
            });

            // Full background rect covering viewBox
            Assert.Contains(@"<rect x=""0"" y=""0"" width=""29"" height=""29""", component.Markup);
        }

        [Fact]
        public void QRCode_AB_Renders_FinderPatternEyes()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenQRCode>(p =>
            {
                p.Add(x => x.Value, "AB");
            });

            // QR codes have 3 finder patterns (eyes) with mask elements
            Assert.Contains("eye-0", component.Markup);
            Assert.Contains("eye-1", component.Markup);
            Assert.Contains("eye-2", component.Markup);
        }

        [Fact]
        public void QRCode_AB_FinderPatterns_CorrectPositions()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenQRCode>(p =>
            {
                p.Add(x => x.Value, "AB");
            });

            // Top-left eye at (4,4), top-right at (18,4), bottom-left at (4,18)
            Assert.Contains(@"<rect x=""4"" y=""4"" width=""7"" height=""7""", component.Markup);
            Assert.Contains(@"<rect x=""18"" y=""4"" width=""7"" height=""7""", component.Markup);
            Assert.Contains(@"<rect x=""4"" y=""18"" width=""7"" height=""7""", component.Markup);
        }

        [Fact]
        public void QRCode_Square_Renders_RectModules()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenQRCode>(p =>
            {
                p.Add(x => x.Value, "AB");
                p.Add(x => x.ModuleShape, QRCodeModuleShape.Square);
            });

            // Square modules use <rect> with width="1" height="1"
            Assert.Contains(@"width=""1"" height=""1""", component.Markup);
            Assert.DoesNotContain("<circle", component.Markup);
        }

        [Fact]
        public void QRCode_Circle_Renders_CircleModules()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenQRCode>(p =>
            {
                p.Add(x => x.Value, "AB");
                p.Add(x => x.ModuleShape, QRCodeModuleShape.Circle);
            });

            // Circle modules use <circle> with r="0.5"
            Assert.Contains(@"<circle cx=""12.5"" cy=""4.5"" r=""0.5""", component.Markup);
            // Data modules should NOT use <rect> (only finder eyes still use rect)
            var circleCount = System.Text.RegularExpressions.Regex.Matches(component.Markup, "<circle").Count;
            Assert.True(circleCount > 50, $"Expected many circle modules, found {circleCount}");
        }

        [Fact]
        public void QRCode_CustomForeground_AppliedToModules()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenQRCode>(p =>
            {
                p.Add(x => x.Value, "AB");
                p.Add(x => x.Foreground, "#FF0000");
            });

            // All modules and eyes should use the custom foreground
            Assert.Contains(@"fill=""#FF0000""", component.Markup);
        }

        [Fact]
        public void QRCode_CustomSize_Applied()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenQRCode>(p =>
            {
                p.Add(x => x.Value, "AB");
                p.Add(x => x.Size, "200px");
            });

            Assert.Contains(@"width=""200px""", component.Markup);
            Assert.Contains(@"height=""200px""", component.Markup);
        }

        [Fact]
        public void QRCode_WithImage_RendersImageElement()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenQRCode>(p =>
            {
                p.Add(x => x.Value, "AB");
                p.Add(x => x.Image, "logo.png");
                p.Add(x => x.Ecc, RadzenQREcc.High);
            });

            Assert.Contains("logo.png", component.Markup);
            Assert.Contains("<image", component.Markup);
        }

        [Fact]
        public void QRCode_DifferentValues_ProduceDifferentModulePatterns()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var c1 = ctx.RenderComponent<RadzenQRCode>(p => p.Add(x => x.Value, "Hello"));
            var c2 = ctx.RenderComponent<RadzenQRCode>(p => p.Add(x => x.Value, "World"));

            // Different data must produce different QR patterns
            Assert.NotEqual(c1.Markup, c2.Markup);
        }

        [Fact]
        public void QRCode_HigherEcc_ProducesLargerMatrix()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var low = ctx.RenderComponent<RadzenQRCode>(p =>
            {
                p.Add(x => x.Value, "ABCDEFGHIJKLMNOP");
                p.Add(x => x.Ecc, RadzenQREcc.Low);
            });
            var high = ctx.RenderComponent<RadzenQRCode>(p =>
            {
                p.Add(x => x.Value, "ABCDEFGHIJKLMNOP");
                p.Add(x => x.Ecc, RadzenQREcc.High);
            });

            // Higher ECC needs more modules, so the viewBox should be larger
            Assert.True(high.Markup.Length > low.Markup.Length,
                "High ECC should produce more SVG content than Low ECC");
        }

        [Fact]
        public void QRCode_CssClass_Applied()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenQRCode>(p =>
            {
                p.Add(x => x.Value, "AB");
            });

            Assert.Contains(@"class=""rz-qrcode""", component.Markup);
        }
    }
}
