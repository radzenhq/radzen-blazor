using Bunit;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class BarcodeTests
    {
        [Fact]
        public void Barcode_Renders_WithClassName()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenBarcode>(parameters =>
            {
                parameters.Add(p => p.Value, "12345");
            });

            Assert.Contains("rz-barcode", component.Markup);
        }

        [Fact]
        public void Barcode_Renders_SvgWithViewBox()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenBarcode>(parameters =>
            {
                parameters.Add(p => p.Value, "TEST");
            });

            Assert.Contains("viewBox=", component.Markup);
            Assert.Contains("shape-rendering=\"crispEdges\"", component.Markup);
            Assert.Contains(@"width=""100%""", component.Markup);
            Assert.Contains(@"height=""80px""", component.Markup);
        }

        [Fact]
        public void Barcode_DefaultType_IsCode128()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenBarcode>();

            Assert.Equal(RadzenBarcodeType.Code128, component.Instance.Type);
        }

        [Fact]
        public void Barcode_HasChecksum_Code128_ReturnsTrue()
        {
            Assert.True(RadzenBarcode.HasChecksum(RadzenBarcodeType.Code128));
        }

        [Fact]
        public void Barcode_HasChecksum_Ean13_ReturnsTrue()
        {
            Assert.True(RadzenBarcode.HasChecksum(RadzenBarcodeType.Ean13));
        }

        [Fact]
        public void Barcode_HasChecksum_Code39_ReturnsFalse()
        {
            Assert.False(RadzenBarcode.HasChecksum(RadzenBarcodeType.Code39));
        }

        [Fact]
        public void Barcode_HasChecksum_Itf_ReturnsFalse()
        {
            Assert.False(RadzenBarcode.HasChecksum(RadzenBarcodeType.Itf));
        }

        [Fact]
        public void Barcode_Renders_DefaultWidth()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenBarcode>(parameters =>
            {
                parameters.Add(p => p.Value, "ABC");
            });

            Assert.Equal("100%", component.Instance.Width);
        }

        [Fact]
        public void Barcode_Renders_DefaultHeight()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenBarcode>(parameters =>
            {
                parameters.Add(p => p.Value, "ABC");
            });

            Assert.Equal("80px", component.Instance.Height);
        }

        [Fact]
        public void Barcode_Renders_DefaultColors()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenBarcode>(parameters =>
            {
                parameters.Add(p => p.Value, "ABC");
            });

            Assert.Equal("#000", component.Instance.Foreground);
            Assert.Equal("#FFF", component.Instance.Background);
        }

        [Fact]
        public void Barcode_Renders_CustomForeground()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenBarcode>(parameters =>
            {
                parameters.Add(p => p.Value, "ABC");
                parameters.Add(p => p.Foreground, "#FF0000");
            });

            Assert.Contains("#FF0000", component.Markup);
        }

        [Fact]
        public void Barcode_Renders_Code39Type()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenBarcode>(parameters =>
            {
                parameters.Add(p => p.Value, "ABC123");
                parameters.Add(p => p.Type, RadzenBarcodeType.Code39);
            });

            // Code39 renders bars as rect elements
            Assert.Contains("<rect", component.Markup);
            Assert.Contains("rz-barcode", component.Markup);
        }
    }
}
