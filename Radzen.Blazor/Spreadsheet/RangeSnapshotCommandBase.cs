using System;
using System.Collections.Generic;

using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet;

#nullable enable

/// <summary>
/// Base class for commands that snapshot a set of cells as detached clones for undo support.
/// </summary>
public abstract class RangeSnapshotCommandBase : ICommand, IProtectedCommand
{
    /// <inheritdoc/>
    public abstract SheetAction RequiredAction { get; }

    /// <inheritdoc/>
    public virtual SpreadsheetFeature? Feature => SpreadsheetFeature.Editing;

    /// <summary>
    /// The sheet being operated on.
    /// </summary>
    protected readonly Worksheet sheet;

    /// <summary>
    /// The captured cell snapshots keyed by cell reference. A snapshot is a detached clone,
    /// or null when the cell did not exist at capture time.
    /// </summary>
    protected readonly Dictionary<CellRef, Cell?> snapshot = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="RangeSnapshotCommandBase"/> class.
    /// </summary>
    protected RangeSnapshotCommandBase(Worksheet sheet)
    {
        this.sheet = sheet ?? throw new ArgumentNullException(nameof(sheet));
    }

    /// <summary>
    /// Captures the cell at <paramref name="cellRef"/> into the snapshot as a detached clone.
    /// </summary>
    protected void Capture(CellRef cellRef)
    {
        snapshot[cellRef] = sheet.Cells.TryGet(cellRef.Row, cellRef.Column, out var cell) ? cell.Clone() : null;
    }

    /// <summary>
    /// Captures every cell in <paramref name="cells"/> into the snapshot.
    /// </summary>
    protected void CaptureRange(IEnumerable<CellRef> cells)
    {
        ArgumentNullException.ThrowIfNull(cells);

        foreach (var cellRef in cells)
        {
            Capture(cellRef);
        }
    }

    /// <summary>
    /// Restores the captured cells back into the sheet.
    /// </summary>
    protected void RestoreSnapshot() => Restore(snapshot);

    /// <summary>
    /// Writes the captured cell state back into the sheet. Cells that did not exist at capture
    /// time are cleared. Batched so dependent formulas recalculate once, even when called
    /// outside the undo stack.
    /// </summary>
    protected void Restore(IReadOnlyDictionary<CellRef, Cell?> cells)
    {
        ArgumentNullException.ThrowIfNull(cells);

        sheet.Batch(() =>
        {
            foreach (var (cellRef, snap) in cells)
            {
                if (snap is null)
                {
                    if (sheet.Cells.TryGet(cellRef.Row, cellRef.Column, out var cell))
                    {
                        cell.Clear();
                    }
                }
                else
                {
                    // Clone again: CopyFrom aliases the format, and the snapshot must stay
                    // isolated from later edits of the live cell.
                    sheet.Cells[cellRef.Row, cellRef.Column].CopyFrom(snap.Clone());
                }
            }
        });
    }

    /// <inheritdoc/>
    public bool Execute()
    {
        snapshot.Clear();
        return DoExecute();
    }

    /// <inheritdoc/>
    public abstract void Unexecute();

    /// <summary>
    /// Performs the command-specific execution. The base class clears the snapshot before this is called.
    /// </summary>
    protected abstract bool DoExecute();
}
