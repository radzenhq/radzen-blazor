using Xunit;

using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet.Tests;

public class DeleteSelectedTests
{
    readonly Worksheet sheet = new(10, 10);

    [Fact]
    public void DeleteSelected_ClearsSelectedCellContents()
    {
        sheet.Cells[1, 1].Value = "Hello";
        sheet.Selection.Select(new CellRef(1, 1));

        var command = new ClearContentsCommand(sheet, sheet.Selection.Range);
        command.Execute();

        Assert.Null(sheet.Cells[1, 1].Value);
    }

    [Fact]
    public void DeleteSelected_PrefersImageDeletion()
    {
        sheet.Cells[0, 0].Value = "Keep";

        var image = new SheetImage
        {
            AnchorMode = DrawingAnchorMode.OneCellAnchor,
            From = new CellAnchor { Row = 0, Column = 0 },
            Width = 100,
            Height = 100,
            Data = [0x89, 0x50, 0x4E, 0x47]
        };

        sheet.AddImage(image);
        sheet.SelectedImage = image;
        sheet.Selection.Select(new CellRef(0, 0));

        // Simulate the DeleteSelectedAsync logic: image deletion takes priority
        var deleteImageCommand = new DeleteImageCommand(sheet, sheet.SelectedImage);
        deleteImageCommand.Execute();

        Assert.Empty(sheet.Images);
        Assert.Null(sheet.SelectedImage);
        Assert.Equal("Keep", sheet.Cells[0, 0].Value);
    }
}
