using Radzen.Blazor.Spreadsheet;
namespace Radzen.Documents.Spreadsheet;

/// <summary>
/// Command to insert an image into a sheet.
/// </summary>
public class InsertImageCommand(Sheet sheet, SheetImage image) : ICommand
{
    private readonly Sheet sheet = sheet;
    private readonly SheetImage image = image;

    /// <inheritdoc/>
    public bool Execute()
    {
        sheet.AddImage(image);
        return true;
    }

    /// <inheritdoc/>
    public void Unexecute()
    {
        sheet.RemoveImage(image);
    }
}
