using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet;

/// <summary>
/// Command to insert an image into a sheet.
/// </summary>
public class InsertImageCommand(Worksheet sheet, SheetImage image) : ICommand
{
    private readonly Worksheet sheet = sheet;
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
