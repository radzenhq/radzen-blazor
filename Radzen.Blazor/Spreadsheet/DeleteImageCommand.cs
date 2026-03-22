using Radzen.Blazor.Spreadsheet;
namespace Radzen.Documents.Spreadsheet;

/// <summary>
/// Command to delete an image from a sheet.
/// </summary>
public class DeleteImageCommand(Sheet sheet, SheetImage image) : ICommand
{
    private readonly Sheet sheet = sheet;
    private readonly SheetImage image = image;

    /// <inheritdoc/>
    public bool Execute()
    {
        sheet.RemoveImage(image);

        if (sheet.SelectedImage == image)
        {
            sheet.SelectedImage = null;
        }

        return true;
    }

    /// <inheritdoc/>
    public void Unexecute()
    {
        sheet.AddImage(image);
    }
}
