#nullable enable

using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Radzen.Documents.Pdf;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class ImageLayoutTests
{
    private static Image Load(string resource)
        => new(PdfTestResources.ReadAllBytes(resource));

    private static (double Width, double Height) Measure(Image image)
        => ImageDecoder.Measure(image, ImageDecoder.Decode(image.Data), double.PositiveInfinity);

    [Fact]
    public void FitInBox_UsesSmallerScale_ForLandscapeBase()
    {
        var image = Load("Images/rgb.jpg");
        image.Width = Unit.FromPoint(100);
        image.Height = Unit.FromPoint(50);
        image.FitInBox(Unit.FromPoint(80), Unit.FromPoint(80));

        var (width, height) = Measure(image);

        // scale = min(80/100, 80/50) = 0.8 -> 80x40, fitting both bounds and keeping the 2:1 aspect.
        Assert.Equal(80, width, 3);
        Assert.Equal(40, height, 3);
        Assert.True(width <= 80 + 1e-6 && height <= 80 + 1e-6);
        Assert.Equal(100.0 / 50.0, width / height, 3);
    }

    [Fact]
    public void FitInBox_UsesSmallerScale_ForPortraitBase()
    {
        var image = Load("Images/rgb.jpg");
        image.Width = Unit.FromPoint(50);
        image.Height = Unit.FromPoint(100);
        image.FitInBox(Unit.FromPoint(80), Unit.FromPoint(80));

        var (width, height) = Measure(image);

        // scale = min(80/50, 80/100) = 0.8 -> 40x80.
        Assert.Equal(40, width, 3);
        Assert.Equal(80, height, 3);
        Assert.True(width <= 80 + 1e-6 && height <= 80 + 1e-6);
        Assert.Equal(50.0 / 100.0, width / height, 3);
    }

    [Fact]
    public void FitInBox_WithoutExplicitSize_FitsNaturalAspect()
    {
        var image = Load("Images/rgb.jpg");
        image.FitInBox(Unit.FromPoint(30), Unit.FromPoint(120));

        var (width, height) = Measure(image);

        // 64x64 natural -> 48x48 pt base; min(30/48, 120/48) = 0.625 -> 30x30.
        Assert.Equal(30, width, 3);
        Assert.Equal(30, height, 3);
    }

    private static double ImageX(DocumentBuilder builder)
    {
        var reader = BuildTestSupport.Read(builder);
        var leaves = BuildTestSupport.PageLeaves(reader);
        var content = Encoding.Latin1.GetString(BuildTestSupport.Content(reader, leaves[0].Page));
        var match = Regex.Match(
            content,
            @"100(?:\.0+)?\s+0\S*\s+0\S*\s+100(?:\.0+)?\s+([-\d.]+)\s+([-\d.]+)\s+cm");
        Assert.True(match.Success, "image placement matrix present");
        return double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
    }

    private static DocumentBuilder AlignedImage(HorizontalAlignment alignment)
    {
        var builder = new DocumentBuilder();
        var section = builder.Sections.Add();
        section.PageSize = new PageSize(Unit.FromPoint(500), Unit.FromPoint(500));
        section.Margin = Unit.FromPoint(0);
        var image = section.Blocks.AddImage(PdfTestResources.Open("Images/rgb.jpg"));
        image.Width = Unit.FromPoint(100);
        image.Height = Unit.FromPoint(100);
        image.Alignment = alignment;
        return builder;
    }

    [Fact]
    public void Alignment_Left_PlacesImageAtContainerLeft()
        => Assert.Equal(0, ImageX(AlignedImage(HorizontalAlignment.Left)), 3);

    [Fact]
    public void Alignment_Center_CentersImage()
        => Assert.Equal(200, ImageX(AlignedImage(HorizontalAlignment.Center)), 3);

    [Fact]
    public void Alignment_Right_PlacesImageAtContainerRight()
        => Assert.Equal(400, ImageX(AlignedImage(HorizontalAlignment.Right)), 3);
}
