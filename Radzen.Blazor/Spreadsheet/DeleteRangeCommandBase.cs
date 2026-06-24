using System;
using System.Linq;

using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet;

#nullable enable

/// <summary>
/// Base class for row and column delete commands. Captures only the cells that will be removed
/// or whose formulas reference the deleted range, then performs the delete. Undo re-inserts
/// the blank rows/columns and restores the captured cells in place. Snapshot/restore plumbing
/// is inherited from <see cref="RangeSnapshotCommandBase"/>.
/// </summary>
public abstract class DeleteRangeCommandBase : RangeSnapshotCommandBase
{
    /// <summary>
    /// The first index in the range to delete (inclusive).
    /// </summary>
    protected readonly int startIndex;

    /// <summary>
    /// The last index in the range to delete (inclusive).
    /// </summary>
    protected readonly int endIndex;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteRangeCommandBase"/> class.
    /// </summary>
    /// <param name="sheet">The worksheet to modify.</param>
    /// <param name="startIndex">The first row/column index to delete (inclusive).</param>
    /// <param name="endIndex">The last row/column index to delete (inclusive).</param>
    protected DeleteRangeCommandBase(Worksheet sheet, int startIndex, int endIndex)
        : base(sheet)
    {
        ArgumentNullException.ThrowIfNull(sheet);
        this.startIndex = startIndex;
        this.endIndex = endIndex;
    }

    /// <summary>
    /// Gets the number of indices in the deletion range.
    /// </summary>
    protected int Count => endIndex - startIndex + 1;

    /// <inheritdoc/>
    protected override bool DoExecute()
    {
        CaptureAffectedCells();
        DeleteRange();
        return true;
    }

    /// <inheritdoc/>
    public override void Unexecute()
    {
        ReinsertRange();
        RestoreSnapshot();
    }

    /// <summary>
    /// Returns true when the cell at <paramref name="address"/> falls inside the deletion range.
    /// </summary>
    protected abstract bool IsInsideRange(CellRef address);

    /// <summary>
    /// Returns true when the formula of <paramref name="cell"/> references any row/column inside the deletion range —
    /// these cells get rewritten to <c>#REF!</c> by the worksheet and must be snapshotted for undo.
    /// </summary>
    protected abstract bool ReferencesRange(Cell cell);

    /// <summary>
    /// Deletes the row/column range from the worksheet.
    /// </summary>
    protected abstract void DeleteRange();

    /// <summary>
    /// Re-inserts <see cref="Count"/> blank rows/columns at <see cref="startIndex"/>.
    /// </summary>
    protected abstract void ReinsertRange();

    private void CaptureAffectedCells()
    {
        foreach (var cell in sheet.Cells.GetPopulatedCells().ToList())
        {
            if (IsInsideRange(cell.Address) || ReferencesRange(cell))
            {
                Capture(cell.Address);
            }
        }
    }
}
