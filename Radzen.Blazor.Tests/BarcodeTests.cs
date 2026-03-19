using Bunit;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class BarcodeTests
    {
        [Fact]
        public void Barcode_Code128_ABC_Renders_CorrectViewBox()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenBarcode>(p =>
            {
                p.Add(x => x.Value, "ABC");
            });

            // Code128 "ABC" produces a viewBox of 90x68 (bars + text area)
            Assert.Contains(@"viewBox=""0 0 90 68""", component.Markup);
        }

        [Fact]
        public void Barcode_Code128_ABC_Renders_BackgroundRect()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenBarcode>(p =>
            {
                p.Add(x => x.Value, "ABC");
            });

            // Full background rect covering entire viewBox
            Assert.Contains(@"<rect x=""0"" y=""0"" width=""90"" height=""68"" fill=""#FFF"">", component.Markup);
        }

        [Fact]
        public void Barcode_Code128_ABC_Renders_CorrectBarCount()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenBarcode>(p =>
            {
                p.Add(x => x.Value, "ABC");
            });

            // Code128 "ABC" = start code B + A + B + C + checksum + stop = specific bar pattern
            // Count bar rects (height="50" are the bars, background rect has height="68")
            var barCount = System.Text.RegularExpressions.Regex.Matches(component.Markup, @"height=""50""").Count;
            Assert.Equal(19, barCount); // 19 bar rects for Code128 "ABC" (start + 3 chars + checksum + stop)
        }

        [Fact]
        public void Barcode_Code128_ABC_ShowsChecksum_InText()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenBarcode>(p =>
            {
                p.Add(x => x.Value, "ABC");
            });

            // Default ShowValue=true, ShowChecksum=true. Code128 checksum for "ABC" is 1
            Assert.Contains(">ABC 1</", component.Markup);
        }

        [Fact]
        public void Barcode_Code39_AB_Renders_CorrectViewBox()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenBarcode>(p =>
            {
                p.Add(x => x.Value, "AB");
                p.Add(x => x.Type, RadzenBarcodeType.Code39);
            });

            Assert.Contains(@"viewBox=""0 0 71 68""", component.Markup);
        }

        [Fact]
        public void Barcode_Code39_AB_ShowsValue_InText()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenBarcode>(p =>
            {
                p.Add(x => x.Value, "AB");
                p.Add(x => x.Type, RadzenBarcodeType.Code39);
            });

            // Code39 has no checksum, so only value text
            Assert.Contains(">AB</", component.Markup);
        }

        [Fact]
        public void Barcode_CustomColors_AppliedToAllBarsAndBackground()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenBarcode>(p =>
            {
                p.Add(x => x.Value, "AB");
                p.Add(x => x.Foreground, "#FF0000");
                p.Add(x => x.Background, "#EEEEFF");
            });

            // Background rect uses custom background color
            Assert.Contains(@"fill=""#EEEEFF""", component.Markup);
            // All bar rects use custom foreground color
            Assert.Contains(@"fill=""#FF0000""", component.Markup);
            // Text also uses foreground color
            Assert.Contains(@"fill=""#FF0000"">AB", component.Markup);
            // No default black bars should remain
            Assert.DoesNotContain(@"fill=""#000""", component.Markup);
        }

        [Fact]
        public void Barcode_ShowValue_False_NoTextElement()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenBarcode>(p =>
            {
                p.Add(x => x.Value, "AB");
                p.Add(x => x.ShowValue, false);
            });

            // No text element when ShowValue is false
            Assert.DoesNotContain("<svg:text", component.Markup);
            // viewBox height should be just bar height (50), no text area
            Assert.Contains(@"viewBox=""0 0 79 50""", component.Markup);
        }

        [Fact]
        public void Barcode_CustomDimensions_Applied()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenBarcode>(p =>
            {
                p.Add(x => x.Value, "AB");
                p.Add(x => x.Width, "300px");
                p.Add(x => x.Height, "100px");
            });

            Assert.Contains(@"width=""300px""", component.Markup);
            Assert.Contains(@"height=""100px""", component.Markup);
        }

        [Fact]
        public void Barcode_HasChecksum_StaticMethod()
        {
            Assert.True(RadzenBarcode.HasChecksum(RadzenBarcodeType.Code128));
            Assert.True(RadzenBarcode.HasChecksum(RadzenBarcodeType.Ean13));
            Assert.True(RadzenBarcode.HasChecksum(RadzenBarcodeType.Msi));
            Assert.False(RadzenBarcode.HasChecksum(RadzenBarcodeType.Code39));
            Assert.False(RadzenBarcode.HasChecksum(RadzenBarcodeType.Itf));
            Assert.False(RadzenBarcode.HasChecksum(RadzenBarcodeType.Codabar));
        }

        [Fact]
        public void Barcode_BarHeight_AffectsRendering()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenBarcode>(p =>
            {
                p.Add(x => x.Value, "AB");
                p.Add(x => x.BarHeight, 80);
                p.Add(x => x.ShowValue, false);
            });

            // Bars should be 80px high
            Assert.Contains(@"height=""80""", component.Markup);
            Assert.DoesNotContain(@"height=""50""", component.Markup);
        }
    }
}
