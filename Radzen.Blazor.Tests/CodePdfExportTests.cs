using System.Linq;
using System.Text;
using Bunit;
using Radzen.Documents.Codes;
using Radzen.Documents.Core;
using Radzen.Documents.Pdf;
using Xunit;

namespace Radzen.Blazor.Tests;

public class CodePdfExportTests
{
    [Fact]
    public void RadzenBarcode_ToPdfDocument_MapsComponentState()
    {
        using var context = new TestContext();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = context.RenderComponent<RadzenBarcode>(parameters => parameters
            .Add(barcode => barcode.Value, "123456789012")
            .Add(barcode => barcode.Type, RadzenBarcodeType.UpcA)
            .Add(barcode => barcode.Width, "200px")
            .Add(barcode => barcode.Height, "96px")
            .Add(barcode => barcode.Foreground, "rgb(10, 20, 30)")
            .Add(barcode => barcode.ShowValue, false));

        var document = component.Instance.ToPdfDocument();
        var barcode = document.Sections[0].Blocks.OfType<Radzen.Documents.Barcode>().Single();

        Assert.Equal(BarcodeType.UpcA, barcode.Type);
        Assert.Equal("123456789012", barcode.Value);
        Assert.Equal(Unit.FromPoint(150), barcode.Width);
        Assert.Equal(Unit.FromPoint(72), barcode.Height);
        Assert.Equal(Color.FromRgb(10, 20, 30), barcode.Foreground);
        Assert.False(barcode.ShowText);
        Assert.Equal(Unit.FromPoint(174), document.Sections[0].PageSize.Width);
        Assert.Equal("%PDF-", Encoding.ASCII.GetString(document.ToPdf(), 0, 5));
    }

    [Fact]
    public void RadzenQRCode_ToPdfDocument_ComposesCenterImageOverlay()
    {
        using var context = new TestContext();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = context.RenderComponent<RadzenQRCode>(parameters => parameters
            .Add(code => code.Value, "https://radzen.com")
            .Add(code => code.Size, "160px")
            .Add(code => code.Image, "images/logo.png")
            .Add(code => code.ImageSizePercent, 25)
            .Add(code => code.ImageBackgroundOpacity, 0.5));

        using var image = new System.IO.MemoryStream(Radzen.Blazor.Pdf.Tests.PdfTestResources.ReadAllBytes("Images/rgb.png"));
        var document = component.Instance.ToPdfDocument(image);
        var overlay = document.Sections[0].Blocks.OfType<Radzen.Documents.Container>().Single();
        var code = overlay.Blocks.OfType<Radzen.Documents.QrCode>().Single();
        var patch = overlay.Blocks.OfType<Radzen.Documents.Container>().Single().Blocks.OfType<Radzen.Documents.Container>().Single();
        var logo = patch.Blocks.OfType<Radzen.Documents.Image>().Single();

        Assert.Equal(Radzen.Documents.ContainerLayout.Overlay, overlay.Layout);
        Assert.Equal(Unit.FromPoint(120), code.Size);
        Assert.Equal(Unit.FromPoint(30), logo.Width);
        Assert.Equal((byte?)128, patch.Background?.A);
        Assert.Equal("%PDF-", Encoding.ASCII.GetString(document.ToPdf(), 0, 5));
    }

    [Fact]
    public void RadzenQRCode_ToPdfDocument_MapsComponentState()
    {
        using var context = new TestContext();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = context.RenderComponent<RadzenQRCode>(parameters => parameters
            .Add(code => code.Value, "https://radzen.com")
            .Add(code => code.Size, "160px")
            .Add(code => code.Ecc, RadzenQREcc.High)
            .Add(code => code.Foreground, "#123456"));

        var document = component.Instance.ToPdfDocument();
        var code = document.Sections[0].Blocks.OfType<Radzen.Documents.Container>().Single().Blocks.OfType<Radzen.Documents.QrCode>().Single();

        Assert.Equal("https://radzen.com", code.Value);
        Assert.Equal(Unit.FromPoint(120), code.Size);
        Assert.Equal(QrErrorCorrection.High, code.ErrorCorrection);
        Assert.Equal(Color.FromRgb(0x12, 0x34, 0x56), code.Foreground);
        Assert.Equal("https://radzen.com", code.AlternateText);
        Assert.Equal("%PDF-", Encoding.ASCII.GetString(document.ToPdf(), 0, 5));
    }
}
