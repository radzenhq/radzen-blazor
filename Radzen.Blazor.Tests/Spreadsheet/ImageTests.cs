using System.IO;
using System.IO.Compression;
using System.Linq;
using Xunit;

using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet.Tests;

public class ImageTests
{
    private static readonly byte[] PngBytes = CreateMinimalPng();
    private static readonly byte[] JpegBytes = CreateMinimalJpeg();

    // Data model tests

    [Fact]
    public void AddImage_AddsToCollection()
    {
        var sheet = new Sheet(10, 10);
        var image = CreateTestImage();

        sheet.AddImage(image);

        Assert.Single(sheet.Images);
        Assert.Same(image, sheet.Images[0]);
    }

    [Fact]
    public void RemoveImage_RemovesFromCollection()
    {
        var sheet = new Sheet(10, 10);
        var image = CreateTestImage();
        sheet.AddImage(image);

        var result = sheet.RemoveImage(image);

        Assert.True(result);
        Assert.Empty(sheet.Images);
    }

    [Fact]
    public void RemoveImage_ReturnsFalse_WhenNotFound()
    {
        var sheet = new Sheet(10, 10);
        var image = CreateTestImage();

        var result = sheet.RemoveImage(image);

        Assert.False(result);
    }

    [Fact]
    public void CellAnchor_Clone_CreatesDeepCopy()
    {
        var anchor = new CellAnchor { Column = 1, ColumnOffset = 100, Row = 2, RowOffset = 200 };
        var clone = anchor.Clone();

        Assert.Equal(1, clone.Column);
        Assert.Equal(100, clone.ColumnOffset);
        Assert.Equal(2, clone.Row);
        Assert.Equal(200, clone.RowOffset);

        clone.Column = 5;
        Assert.Equal(1, anchor.Column);
    }

    [Fact]
    public void Images_Empty_ByDefault()
    {
        var sheet = new Sheet(10, 10);
        Assert.Empty(sheet.Images);
    }

    [Fact]
    public void SelectedImage_Null_ByDefault()
    {
        var sheet = new Sheet(10, 10);
        Assert.Null(sheet.SelectedImage);
    }

    // Command tests

    [Fact]
    public void InsertImageCommand_Execute_AddsImage()
    {
        var sheet = new Sheet(10, 10);
        var image = CreateTestImage();
        var command = new InsertImageCommand(sheet, image);

        Assert.True(command.Execute());
        Assert.Single(sheet.Images);
    }

    [Fact]
    public void InsertImageCommand_Unexecute_RemovesImage()
    {
        var sheet = new Sheet(10, 10);
        var image = CreateTestImage();
        var command = new InsertImageCommand(sheet, image);
        command.Execute();

        command.Unexecute();

        Assert.Empty(sheet.Images);
    }

    [Fact]
    public void DeleteImageCommand_Execute_RemovesImage()
    {
        var sheet = new Sheet(10, 10);
        var image = CreateTestImage();
        sheet.AddImage(image);
        var command = new DeleteImageCommand(sheet, image);

        Assert.True(command.Execute());
        Assert.Empty(sheet.Images);
    }

    [Fact]
    public void DeleteImageCommand_Execute_ClearsSelectedImage()
    {
        var sheet = new Sheet(10, 10);
        var image = CreateTestImage();
        sheet.AddImage(image);
        sheet.SelectedImage = image;
        var command = new DeleteImageCommand(sheet, image);

        command.Execute();

        Assert.Null(sheet.SelectedImage);
    }

    [Fact]
    public void DeleteImageCommand_Unexecute_RestoresImage()
    {
        var sheet = new Sheet(10, 10);
        var image = CreateTestImage();
        sheet.AddImage(image);
        var command = new DeleteImageCommand(sheet, image);
        command.Execute();

        command.Unexecute();

        Assert.Single(sheet.Images);
    }

    [Fact]
    public void MoveImageCommand_Execute_UpdatesAnchors()
    {
        var image = CreateTestImage();
        var newFrom = new CellAnchor { Row = 5, Column = 3, RowOffset = 100, ColumnOffset = 200 };
        var command = new MoveImageCommand(image, newFrom, null);

        command.Execute();

        Assert.Equal(5, image.From.Row);
        Assert.Equal(3, image.From.Column);
    }

    [Fact]
    public void MoveImageCommand_Unexecute_RestoresAnchors()
    {
        var image = CreateTestImage();
        var originalRow = image.From.Row;
        var originalCol = image.From.Column;
        var newFrom = new CellAnchor { Row = 5, Column = 3 };
        var command = new MoveImageCommand(image, newFrom, null);
        command.Execute();

        command.Unexecute();

        Assert.Equal(originalRow, image.From.Row);
        Assert.Equal(originalCol, image.From.Column);
    }

    [Fact]
    public void ResizeImageCommand_OneCellAnchor_Execute_UpdatesSize()
    {
        var image = CreateTestImage();
        image.AnchorMode = ImageAnchorMode.OneCellAnchor;
        image.Width = 1000;
        image.Height = 2000;
        var command = new ResizeImageCommand(image, 3000, 4000);

        command.Execute();

        Assert.Equal(3000, image.Width);
        Assert.Equal(4000, image.Height);
    }

    [Fact]
    public void ResizeImageCommand_OneCellAnchor_Unexecute_RestoresSize()
    {
        var image = CreateTestImage();
        image.AnchorMode = ImageAnchorMode.OneCellAnchor;
        image.Width = 1000;
        image.Height = 2000;
        var command = new ResizeImageCommand(image, 3000, 4000);
        command.Execute();

        command.Unexecute();

        Assert.Equal(1000, image.Width);
        Assert.Equal(2000, image.Height);
    }

    [Fact]
    public void ResizeImageCommand_TwoCellAnchor_Execute_UpdatesTo()
    {
        var image = CreateTwoCellImage();
        var newTo = new CellAnchor { Row = 10, Column = 8, RowOffset = 500, ColumnOffset = 600 };
        var command = new ResizeImageCommand(image, newTo);

        command.Execute();

        Assert.Equal(10, image.To!.Row);
        Assert.Equal(8, image.To.Column);
    }

    [Fact]
    public void ResizeImageCommand_TwoCellAnchor_Unexecute_RestoresTo()
    {
        var image = CreateTwoCellImage();
        var originalToRow = image.To!.Row;
        var originalToCol = image.To.Column;
        var newTo = new CellAnchor { Row = 10, Column = 8 };
        var command = new ResizeImageCommand(image, newTo);
        command.Execute();

        command.Unexecute();

        Assert.Equal(originalToRow, image.To!.Row);
        Assert.Equal(originalToCol, image.To.Column);
    }

    // XLSX round-trip tests

    [Fact]
    public void RoundTrip_OneCellAnchor_PreservesPositionAndSize()
    {
        var workbook = new Workbook();
        var sheet = new Sheet(20, 10);
        workbook.AddSheet(sheet);

        var image = new SheetImage
        {
            AnchorMode = ImageAnchorMode.OneCellAnchor,
            From = new CellAnchor { Row = 2, Column = 3, RowOffset = 12345, ColumnOffset = 67890 },
            Width = 2000000,
            Height = 1500000,
            Data = PngBytes,
            ContentType = "image/png",
            Name = "test.png"
        };
        sheet.AddImage(image);

        var reimported = ExportAndReimport(workbook);
        var reimportedSheet = reimported.Sheets[0];

        Assert.Single(reimportedSheet.Images);
        var reimportedImage = reimportedSheet.Images[0];
        Assert.Equal(ImageAnchorMode.OneCellAnchor, reimportedImage.AnchorMode);
        Assert.Equal(2, reimportedImage.From.Row);
        Assert.Equal(3, reimportedImage.From.Column);
        Assert.Equal(12345, reimportedImage.From.RowOffset);
        Assert.Equal(67890, reimportedImage.From.ColumnOffset);
        Assert.Equal(2000000, reimportedImage.Width);
        Assert.Equal(1500000, reimportedImage.Height);
        Assert.Equal("image/png", reimportedImage.ContentType);
        Assert.Equal(PngBytes, reimportedImage.Data);
    }

    [Fact]
    public void RoundTrip_TwoCellAnchor_PreservesAnchors()
    {
        var workbook = new Workbook();
        var sheet = new Sheet(20, 10);
        workbook.AddSheet(sheet);

        var image = new SheetImage
        {
            AnchorMode = ImageAnchorMode.TwoCellAnchor,
            From = new CellAnchor { Row = 1, Column = 2, RowOffset = 100, ColumnOffset = 200 },
            To = new CellAnchor { Row = 5, Column = 6, RowOffset = 300, ColumnOffset = 400 },
            Data = PngBytes,
            ContentType = "image/png",
            Name = "chart.png"
        };
        sheet.AddImage(image);

        var reimported = ExportAndReimport(workbook);
        var reimportedImage = reimported.Sheets[0].Images[0];

        Assert.Equal(ImageAnchorMode.TwoCellAnchor, reimportedImage.AnchorMode);
        Assert.Equal(1, reimportedImage.From.Row);
        Assert.Equal(2, reimportedImage.From.Column);
        Assert.Equal(100, reimportedImage.From.RowOffset);
        Assert.Equal(200, reimportedImage.From.ColumnOffset);
        Assert.NotNull(reimportedImage.To);
        Assert.Equal(5, reimportedImage.To.Row);
        Assert.Equal(6, reimportedImage.To.Column);
        Assert.Equal(300, reimportedImage.To.RowOffset);
        Assert.Equal(400, reimportedImage.To.ColumnOffset);
    }

    [Fact]
    public void RoundTrip_MultipleImages_AllPreserved()
    {
        var workbook = new Workbook();
        var sheet = new Sheet(20, 10);
        workbook.AddSheet(sheet);

        for (var i = 0; i < 3; i++)
        {
            sheet.AddImage(new SheetImage
            {
                AnchorMode = ImageAnchorMode.OneCellAnchor,
                From = new CellAnchor { Row = i, Column = i },
                Width = 1000000,
                Height = 1000000,
                Data = PngBytes,
                ContentType = "image/png",
                Name = $"image{i}.png"
            });
        }

        var reimported = ExportAndReimport(workbook);

        Assert.Equal(3, reimported.Sheets[0].Images.Count);
    }

    [Fact]
    public void RoundTrip_NoImages_EmptyCollection()
    {
        var workbook = new Workbook();
        var sheet = new Sheet(10, 5);
        workbook.AddSheet(sheet);

        var reimported = ExportAndReimport(workbook);

        Assert.Empty(reimported.Sheets[0].Images);
    }

    [Fact]
    public void RoundTrip_JpegImage_ContentTypePreserved()
    {
        var workbook = new Workbook();
        var sheet = new Sheet(20, 10);
        workbook.AddSheet(sheet);

        sheet.AddImage(new SheetImage
        {
            AnchorMode = ImageAnchorMode.OneCellAnchor,
            From = new CellAnchor { Row = 0, Column = 0 },
            Width = 1000000,
            Height = 1000000,
            Data = JpegBytes,
            ContentType = "image/jpeg",
            Name = "photo.jpg"
        });

        var reimported = ExportAndReimport(workbook);
        Assert.Equal("image/jpeg", reimported.Sheets[0].Images[0].ContentType);
    }

    [Fact]
    public void RoundTrip_ImageDeduplication_SameBytesOneMediaFile()
    {
        var workbook = new Workbook();
        var sheet = new Sheet(20, 10);
        workbook.AddSheet(sheet);

        // Add two images with the same data
        sheet.AddImage(new SheetImage
        {
            AnchorMode = ImageAnchorMode.OneCellAnchor,
            From = new CellAnchor { Row = 0, Column = 0 },
            Width = 1000000,
            Height = 1000000,
            Data = PngBytes,
            ContentType = "image/png",
            Name = "image1.png"
        });

        sheet.AddImage(new SheetImage
        {
            AnchorMode = ImageAnchorMode.OneCellAnchor,
            From = new CellAnchor { Row = 5, Column = 5 },
            Width = 1000000,
            Height = 1000000,
            Data = PngBytes,
            ContentType = "image/png",
            Name = "image2.png"
        });

        using var ms = new MemoryStream();
        workbook.SaveToStream(ms);
        ms.Position = 0;

        using var archive = new ZipArchive(ms, ZipArchiveMode.Read);
        var mediaEntries = archive.Entries.Where(e => e.FullName.StartsWith("xl/media/")).ToList();

        Assert.Single(mediaEntries);
    }

    [Fact]
    public void RoundTrip_ImageName_Preserved()
    {
        var workbook = new Workbook();
        var sheet = new Sheet(20, 10);
        workbook.AddSheet(sheet);

        sheet.AddImage(new SheetImage
        {
            AnchorMode = ImageAnchorMode.OneCellAnchor,
            From = new CellAnchor { Row = 0, Column = 0 },
            Width = 1000000,
            Height = 1000000,
            Data = PngBytes,
            ContentType = "image/png",
            Name = "MyLogo"
        });

        var reimported = ExportAndReimport(workbook);
        Assert.Equal("MyLogo", reimported.Sheets[0].Images[0].Name);
    }

    [Fact]
    public void RoundTrip_ImageData_PreservedExactly()
    {
        var workbook = new Workbook();
        var sheet = new Sheet(20, 10);
        workbook.AddSheet(sheet);

        sheet.AddImage(new SheetImage
        {
            AnchorMode = ImageAnchorMode.OneCellAnchor,
            From = new CellAnchor { Row = 0, Column = 0 },
            Width = 1000000,
            Height = 1000000,
            Data = PngBytes,
            ContentType = "image/png"
        });

        var reimported = ExportAndReimport(workbook);
        Assert.Equal(PngBytes, reimported.Sheets[0].Images[0].Data);
    }

    [Fact]
    public void RoundTrip_ViaFile_DoesNotCorruptZip()
    {
        var workbook = new Workbook();
        var sheet = new Sheet(20, 10);
        workbook.AddSheet(sheet);
        sheet.Cells[0, 0].Value = "test";

        sheet.AddImage(new SheetImage
        {
            AnchorMode = ImageAnchorMode.OneCellAnchor,
            From = new CellAnchor { Row = 2, Column = 1 },
            Width = 2000000,
            Height = 1500000,
            Data = PngBytes,
            ContentType = "image/png",
            Name = "test.png"
        });

        var reimported = ExportAndReimportViaFile(workbook);
        Assert.Single(reimported.Sheets[0].Images);
        Assert.Equal(PngBytes, reimported.Sheets[0].Images[0].Data);
    }

    // Helpers

    private static SheetImage CreateTestImage()
    {
        return new SheetImage
        {
            AnchorMode = ImageAnchorMode.OneCellAnchor,
            From = new CellAnchor { Row = 0, Column = 0 },
            Width = 1000000,
            Height = 1000000,
            Data = PngBytes,
            ContentType = "image/png",
            Name = "test.png"
        };
    }

    private static SheetImage CreateTwoCellImage()
    {
        return new SheetImage
        {
            AnchorMode = ImageAnchorMode.TwoCellAnchor,
            From = new CellAnchor { Row = 0, Column = 0, RowOffset = 10, ColumnOffset = 20 },
            To = new CellAnchor { Row = 5, Column = 5, RowOffset = 30, ColumnOffset = 40 },
            Data = PngBytes,
            ContentType = "image/png",
            Name = "test.png"
        };
    }

    private static Workbook ExportAndReimport(Workbook workbook)
    {
        using var ms = new MemoryStream();
        workbook.SaveToStream(ms);
        ms.Position = 0;
        return Workbook.LoadFromStream(ms);
    }

    /// <summary>
    /// File-backed round-trip that catches ZIP corruption not visible with MemoryStream.
    /// </summary>
    private static Workbook ExportAndReimportViaFile(Workbook workbook)
    {
        var path = Path.GetTempFileName();
        try
        {
            using (var fs = File.Create(path))
            {
                workbook.SaveToStream(fs);
            }
            using var readFs = File.OpenRead(path);
            return Workbook.LoadFromStream(readFs);
        }
        finally
        {
            File.Delete(path);
        }
    }

    // Minimal valid PNG (1x1 transparent pixel)
    private static byte[] CreateMinimalPng()
    {
        return
        [
            0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, // PNG signature
            0x00, 0x00, 0x00, 0x0D, // IHDR length
            0x49, 0x48, 0x44, 0x52, // IHDR
            0x00, 0x00, 0x00, 0x01, // width = 1
            0x00, 0x00, 0x00, 0x01, // height = 1
            0x08, 0x06, 0x00, 0x00, 0x00, // 8-bit RGBA
            0x1F, 0x15, 0xC4, 0x89, // CRC
            0x00, 0x00, 0x00, 0x0A, // IDAT length
            0x49, 0x44, 0x41, 0x54, // IDAT
            0x78, 0x9C, 0x62, 0x00, 0x00, 0x00, 0x02, 0x00, 0x01, // compressed data
            0xE5, 0x27, 0xDE, 0xFC, // CRC
            0x00, 0x00, 0x00, 0x00, // IEND length
            0x49, 0x45, 0x4E, 0x44, // IEND
            0xAE, 0x42, 0x60, 0x82  // CRC
        ];
    }

    // Minimal JPEG bytes (just enough to be a valid-ish file)
    private static byte[] CreateMinimalJpeg()
    {
        return [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01, 0x01, 0x00, 0x00, 0x01, 0x00, 0x01, 0x00, 0x00, 0xFF, 0xD9];
    }
}
