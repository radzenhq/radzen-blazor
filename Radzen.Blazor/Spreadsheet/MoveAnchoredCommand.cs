using Radzen.Documents.Spreadsheet;

namespace Radzen.Blazor.Spreadsheet;

/// <summary>
/// Command to move an anchored drawing (image or chart) to new anchor positions.
/// </summary>
/// <typeparam name="T">The anchored drawing type.</typeparam>
public class MoveAnchoredCommand<T>(T drawing, CellAnchor newFrom, CellAnchor? newTo, SpreadsheetFeature feature) : ICommand
    where T : IAnchoredDrawing
{
    /// <inheritdoc/>
    public SpreadsheetFeature? Feature { get; } = feature;

    private readonly T drawing = drawing;
    private readonly CellAnchor newFrom = newFrom;
    private readonly CellAnchor? newTo = newTo;
    private CellAnchor? oldFrom;
    private CellAnchor? oldTo;

    /// <inheritdoc/>
    public bool Execute()
    {
        oldFrom = drawing.From.Clone();
        oldTo = drawing.To?.Clone();

        drawing.From = newFrom.Clone();

        if (newTo is not null)
        {
            drawing.To = newTo.Clone();
        }

        return true;
    }

    /// <inheritdoc/>
    public void Unexecute()
    {
        drawing.From = oldFrom!.Clone();

        if (oldTo is not null)
        {
            drawing.To = oldTo.Clone();
        }
    }
}
