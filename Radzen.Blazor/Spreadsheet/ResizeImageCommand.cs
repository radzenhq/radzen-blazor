using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet;

/// <summary>
/// Command to resize an image.
/// </summary>
public class ResizeImageCommand : ICommand
{
    private readonly SheetImage image;
    private readonly CellAnchor? newFrom;
    private readonly CellAnchor? newTo;
    private readonly double newWidth;
    private readonly double newHeight;
    private CellAnchor? oldFrom;
    private CellAnchor? oldTo;
    private double oldWidth;
    private double oldHeight;

    /// <summary>
    /// Creates a resize command for a TwoCellAnchor image.
    /// </summary>
    public ResizeImageCommand(SheetImage image, CellAnchor newTo)
    {
        this.image = image;
        this.newTo = newTo;
    }

    /// <summary>
    /// Creates a resize command for a OneCellAnchor image. Optionally moves the
    /// From anchor when resizing from the left or top edge.
    /// </summary>
    public ResizeImageCommand(SheetImage image, double newWidth, double newHeight, CellAnchor? newFrom = null)
    {
        this.image = image;
        this.newWidth = newWidth;
        this.newHeight = newHeight;
        this.newFrom = newFrom;
    }

    /// <inheritdoc/>
    public bool Execute()
    {
        if (newTo is not null)
        {
            oldTo = image.To?.Clone();
            image.To = newTo.Clone();
        }
        else
        {
            oldWidth = image.Width;
            oldHeight = image.Height;
            image.Width = newWidth;
            image.Height = newHeight;

            if (newFrom is not null)
            {
                oldFrom = image.From.Clone();
                image.From = newFrom.Clone();
            }
        }

        return true;
    }

    /// <inheritdoc/>
    public void Unexecute()
    {
        if (newTo is not null)
        {
            image.To = oldTo?.Clone();
        }
        else
        {
            image.Width = oldWidth;
            image.Height = oldHeight;

            if (oldFrom is not null)
            {
                image.From = oldFrom.Clone();
            }
        }
    }
}
