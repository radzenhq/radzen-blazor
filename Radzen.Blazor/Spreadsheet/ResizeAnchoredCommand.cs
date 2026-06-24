using System;
using Radzen.Documents.Spreadsheet;

namespace Radzen.Blazor.Spreadsheet;

/// <summary>
/// Command to resize an anchored drawing (image or chart).
/// </summary>
/// <typeparam name="T">The anchored drawing type.</typeparam>
public class ResizeAnchoredCommand<T> : ICommand
    where T : IAnchoredDrawing
{
    /// <inheritdoc/>
    public SpreadsheetFeature? Feature { get; }

    private readonly T drawing;
    private readonly CellAnchor? newFrom;
    private readonly CellAnchor? newTo;
    private readonly double newWidth;
    private readonly double newHeight;
    private CellAnchor? oldFrom;
    private CellAnchor? oldTo;
    private double oldWidth;
    private double oldHeight;

    /// <summary>
    /// Creates a resize command for a TwoCellAnchor drawing.
    /// </summary>
    public ResizeAnchoredCommand(T drawing, CellAnchor newTo, SpreadsheetFeature feature)
    {
        ArgumentNullException.ThrowIfNull(newTo);

        this.drawing = drawing;
        this.newTo = newTo;
        Feature = feature;
    }

    /// <summary>
    /// Creates a resize command for a OneCellAnchor drawing. Optionally moves the
    /// From anchor when resizing from the left or top edge.
    /// </summary>
    public ResizeAnchoredCommand(T drawing, double newWidth, double newHeight, SpreadsheetFeature feature, CellAnchor? newFrom = null)
    {
        this.drawing = drawing;
        this.newWidth = newWidth;
        this.newHeight = newHeight;
        this.newFrom = newFrom;
        Feature = feature;
    }

    /// <inheritdoc/>
    public bool Execute()
    {
        if (newTo is not null)
        {
            oldTo = drawing.To?.Clone();
            drawing.To = newTo.Clone();
        }
        else
        {
            oldWidth = drawing.Width;
            oldHeight = drawing.Height;
            drawing.Width = newWidth;
            drawing.Height = newHeight;

            if (newFrom is not null)
            {
                oldFrom = drawing.From.Clone();
                drawing.From = newFrom.Clone();
            }
        }

        return true;
    }

    /// <inheritdoc/>
    public void Unexecute()
    {
        if (newTo is not null)
        {
            drawing.To = oldTo?.Clone();
        }
        else
        {
            drawing.Width = oldWidth;
            drawing.Height = oldHeight;

            if (oldFrom is not null)
            {
                drawing.From = oldFrom.Clone();
            }
        }
    }
}
