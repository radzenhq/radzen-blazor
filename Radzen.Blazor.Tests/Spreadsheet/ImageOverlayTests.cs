using Bunit;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Radzen.Blazor.Spreadsheet.Tests;

public class ImageOverlayTests : TestContext
{
    private static readonly byte[] PngBytes =
    [
        0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A,
        0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52,
        0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01,
        0x08, 0x06, 0x00, 0x00, 0x00, 0x1F, 0x15, 0xC4,
        0x89, 0x00, 0x00, 0x00, 0x0A, 0x49, 0x44, 0x41,
        0x54, 0x78, 0x9C, 0x62, 0x00, 0x00, 0x00, 0x02,
        0x00, 0x01, 0xE5, 0x27, 0xDE, 0xFC, 0x00, 0x00,
        0x00, 0x00, 0x49, 0x45, 0x4E, 0x44, 0xAE, 0x42,
        0x60, 0x82
    ];

    [Fact]
    public void ImageOverlay_RendersNothing_WhenNoImages()
    {
        var sheet = new Sheet(10, 10);
        var context = new ImageMockContext();

        var cut = RenderComponent<ImageOverlay>(parameters => parameters
            .Add(p => p.Sheet, sheet)
            .Add(p => p.Context, context));

        Assert.Empty(cut.FindAll(".rz-spreadsheet-image"));
    }

    [Fact]
    public void ImageOverlay_RendersDivAndImg_ForEachImage()
    {
        var sheet = new Sheet(10, 10);
        sheet.AddImage(CreateImage(0, 0));
        sheet.AddImage(CreateImage(2, 2));
        var context = new ImageMockContext();

        var cut = RenderComponent<ImageOverlay>(parameters => parameters
            .Add(p => p.Sheet, sheet)
            .Add(p => p.Context, context));

        var divs = cut.FindAll(".rz-spreadsheet-image");
        Assert.Equal(2, divs.Count);

        var imgs = cut.FindAll("img");
        Assert.Equal(2, imgs.Count);
    }

    [Fact]
    public void ImageOverlay_HasCorrectCssClass()
    {
        var sheet = new Sheet(10, 10);
        sheet.AddImage(CreateImage(0, 0));
        var context = new ImageMockContext();

        var cut = RenderComponent<ImageOverlay>(parameters => parameters
            .Add(p => p.Sheet, sheet)
            .Add(p => p.Context, context));

        var element = cut.Find("div");
        Assert.Contains("rz-spreadsheet-image", element.ClassName);
    }

    [Fact]
    public void ImageOverlay_AppliesFrozenColumnClass()
    {
        var sheet = new Sheet(10, 10);
        sheet.Columns.Frozen = 2;
        sheet.AddImage(CreateImage(0, 1)); // column 1 < frozen 2
        var context = new ImageMockContext();

        var cut = RenderComponent<ImageOverlay>(parameters => parameters
            .Add(p => p.Sheet, sheet)
            .Add(p => p.Context, context));

        var element = cut.Find(".rz-spreadsheet-image");
        Assert.Contains("rz-spreadsheet-frozen-column", element.ClassName);
    }

    [Fact]
    public void ImageOverlay_AppliesFrozenRowClass()
    {
        var sheet = new Sheet(10, 10);
        sheet.Rows.Frozen = 2;
        sheet.AddImage(CreateImage(1, 0)); // row 1 < frozen 2
        var context = new ImageMockContext();

        var cut = RenderComponent<ImageOverlay>(parameters => parameters
            .Add(p => p.Sheet, sheet)
            .Add(p => p.Context, context));

        var element = cut.Find(".rz-spreadsheet-image");
        Assert.Contains("rz-spreadsheet-frozen-row", element.ClassName);
    }

    [Fact]
    public void ImageOverlay_NoFrozenClass_WhenNotInFrozenArea()
    {
        var sheet = new Sheet(10, 10);
        sheet.Rows.Frozen = 1;
        sheet.Columns.Frozen = 1;
        sheet.AddImage(CreateImage(2, 2)); // row 2, col 2 - not frozen
        var context = new ImageMockContext();

        var cut = RenderComponent<ImageOverlay>(parameters => parameters
            .Add(p => p.Sheet, sheet)
            .Add(p => p.Context, context));

        var element = cut.Find(".rz-spreadsheet-image");
        Assert.DoesNotContain("rz-spreadsheet-frozen-row", element.ClassName);
        Assert.DoesNotContain("rz-spreadsheet-frozen-column", element.ClassName);
    }

    [Fact]
    public void ImageOverlay_DataUri_ContainsCorrectBase64AndContentType()
    {
        var sheet = new Sheet(10, 10);
        sheet.AddImage(CreateImage(0, 0));
        var context = new ImageMockContext();

        var cut = RenderComponent<ImageOverlay>(parameters => parameters
            .Add(p => p.Sheet, sheet)
            .Add(p => p.Context, context));

        var img = cut.Find("img");
        var src = img.GetAttribute("src");
        Assert.StartsWith("data:image/png;base64,", src);

        var base64 = src!.Substring("data:image/png;base64,".Length);
        var decoded = Convert.FromBase64String(base64);
        Assert.Equal(PngBytes, decoded);
    }

    [Fact]
    public void ImageOverlay_SelectedImage_HasSelectedClass()
    {
        var sheet = new Sheet(10, 10);
        var image = CreateImage(0, 0);
        sheet.AddImage(image);
        sheet.SelectedImage = image;
        var context = new ImageMockContext();

        var cut = RenderComponent<ImageOverlay>(parameters => parameters
            .Add(p => p.Sheet, sheet)
            .Add(p => p.Context, context));

        var element = cut.Find(".rz-spreadsheet-image");
        Assert.Contains("rz-spreadsheet-image-selected", element.ClassName);
    }

    private static SheetImage CreateImage(int row, int column)
    {
        return new SheetImage
        {
            AnchorMode = ImageAnchorMode.OneCellAnchor,
            From = new CellAnchor { Row = row, Column = column },
            Width = 1000000,
            Height = 1000000,
            Data = PngBytes,
            ContentType = "image/png",
            Name = "test.png"
        };
    }
}

public class ImageMockContext : IVirtualGridContext
{
    public PixelRectangle GetRectangle(int row, int column)
    {
        return new PixelRectangle(row * 24, column * 100, (row + 1) * 24, (column + 1) * 100);
    }

    public PixelRectangle GetRectangle(int top, int left, int bottom, int right)
    {
        return new PixelRectangle(new PixelRange(left * 100, (right + 1) * 100), new PixelRange(top * 24, (bottom + 1) * 24));
    }
}
