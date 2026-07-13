#nullable enable
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;

using Radzen.Documents.Pdf.Emit;
namespace Radzen.Blazor.Pdf.Tests;

public class JpegPassthroughTests
{
    [Fact]
    public void Rgb_DctDecodePassthrough()
    {
        var bytes = PdfTestResources.ReadAllBytes("Images/rgb.jpg");
        var xobj = ImageDecoder.Decode(bytes);
        var dict = xobj.Image.Dictionary;

        Assert.Equal("XObject", ImageTestHelpers.Name(dict, "Type"));
        Assert.Equal("Image", ImageTestHelpers.Name(dict, "Subtype"));
        Assert.Equal(64, ImageTestHelpers.Int(dict, "Width"));
        Assert.Equal(64, ImageTestHelpers.Int(dict, "Height"));
        Assert.Equal("DeviceRGB", ImageTestHelpers.Name(dict, "ColorSpace"));
        Assert.Equal(8, ImageTestHelpers.Int(dict, "BitsPerComponent"));
        Assert.Equal("DCTDecode", ImageTestHelpers.Name(dict, "Filter"));

        Assert.Equal(bytes, xobj.Image.Data);
        Assert.Null(xobj.SoftMask);
    }

    [Fact]
    public void Gray_DctDecodePassthrough()
    {
        var bytes = PdfTestResources.ReadAllBytes("Images/gray.jpg");
        var xobj = ImageDecoder.Decode(bytes);
        var dict = xobj.Image.Dictionary;

        Assert.Equal("DeviceGray", ImageTestHelpers.Name(dict, "ColorSpace"));
        Assert.Equal("DCTDecode", ImageTestHelpers.Name(dict, "Filter"));
        Assert.Equal(8, ImageTestHelpers.Int(dict, "BitsPerComponent"));
        Assert.Equal(64, ImageTestHelpers.Int(dict, "Width"));
        Assert.Equal(64, ImageTestHelpers.Int(dict, "Height"));

        Assert.Equal(bytes, xobj.Image.Data);
        Assert.Null(xobj.SoftMask);
    }

    [Fact]
    public void Cmyk_DctDecodeWithAdobeInvertedDecode()
    {
        var bytes = PdfTestResources.ReadAllBytes("Images/cmyk.jpg");
        var xobj = ImageDecoder.Decode(bytes);
        var dict = xobj.Image.Dictionary;

        Assert.Equal("DeviceCMYK", ImageTestHelpers.Name(dict, "ColorSpace"));
        Assert.Equal("DCTDecode", ImageTestHelpers.Name(dict, "Filter"));
        Assert.Equal(8, ImageTestHelpers.Int(dict, "BitsPerComponent"));

        var decode = Assert.IsType<ArrayObject>(dict["Decode"]);
        Assert.Equal(8, decode.Count);
        int[] expected = [1, 0, 1, 0, 1, 0, 1, 0];
        for (var i = 0; i < expected.Length; i++)
        {
            Assert.Equal(expected[i], Assert.IsType<NumberObject>(decode[i]).IntValue);
        }

        Assert.Equal(bytes, xobj.Image.Data);
    }
}
