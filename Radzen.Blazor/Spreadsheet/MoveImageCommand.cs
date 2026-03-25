using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet;

/// <summary>
/// Command to move an image to new anchor positions.
/// </summary>
public class MoveImageCommand(SheetImage image, CellAnchor newFrom, CellAnchor? newTo) : ICommand
{
    private readonly SheetImage image = image;
    private readonly CellAnchor newFrom = newFrom;
    private readonly CellAnchor? newTo = newTo;
    private CellAnchor? oldFrom;
    private CellAnchor? oldTo;

    /// <inheritdoc/>
    public bool Execute()
    {
        oldFrom = image.From.Clone();
        oldTo = image.To?.Clone();

        image.From = newFrom.Clone();

        if (newTo is not null)
        {
            image.To = newTo.Clone();
        }

        return true;
    }

    /// <inheritdoc/>
    public void Unexecute()
    {
        if (oldFrom is not null)
        {
            image.From = oldFrom.Clone();
        }

        if (oldTo is not null)
        {
            image.To = oldTo.Clone();
        }
    }
}
