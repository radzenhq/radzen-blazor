namespace Radzen.Blazor.Spreadsheet;

/// <summary>
/// Command to resize an image.
/// </summary>
public class ResizeImageCommand : ICommand
{
    private readonly SheetImage image;
    private readonly CellAnchor? newTo;
    private readonly long newWidth;
    private readonly long newHeight;
    private CellAnchor? oldTo;
    private long oldWidth;
    private long oldHeight;

    /// <summary>
    /// Creates a resize command for a TwoCellAnchor image.
    /// </summary>
    public ResizeImageCommand(SheetImage image, CellAnchor newTo)
    {
        this.image = image;
        this.newTo = newTo;
    }

    /// <summary>
    /// Creates a resize command for a OneCellAnchor image.
    /// </summary>
    public ResizeImageCommand(SheetImage image, long newWidth, long newHeight)
    {
        this.image = image;
        this.newWidth = newWidth;
        this.newHeight = newHeight;
    }

    /// <inheritdoc/>
    public bool Execute()
    {
        if (newTo != null)
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
        }

        return true;
    }

    /// <inheritdoc/>
    public void Unexecute()
    {
        if (newTo != null)
        {
            image.To = oldTo?.Clone();
        }
        else
        {
            image.Width = oldWidth;
            image.Height = oldHeight;
        }
    }
}
