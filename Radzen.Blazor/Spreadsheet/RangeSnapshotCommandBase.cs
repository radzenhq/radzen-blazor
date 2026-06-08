using System;
using System.Collections.Generic;

using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet;

#nullable enable

/// <summary>
/// Base class for commands that snapshot a set of cells (value, formula, format) for undo support.
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
    /// The captured cell snapshots keyed by cell reference.
    /// </summary>
    protected readonly Dictionary<CellRef, (object? value, string? formula, Format? format)> snapshot = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="RangeSnapshotCommandBase"/> class.
    /// </summary>
    protected RangeSnapshotCommandBase(Worksheet sheet)
    {
        this.sheet = sheet ?? throw new ArgumentNullException(nameof(sheet));
    }

    /// <summary>
    /// Captures the current value, formula and format of the cell at <paramref name="cellRef"/> into the snapshot.
    /// </summary>
    protected void Capture(CellRef cellRef)
    {
        object? value = null;
        string? formula = null;
        Format? format = null;

        if (sheet.Cells.TryGet(cellRef.Row, cellRef.Column, out var cell))
        {
            value = cell.Value;
            formula = cell.Formula;
            format = cell.Format?.Clone();
        }

        snapshot[cellRef] = (value, formula, format);
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
    /// Restores the captured cells back into the sheet using the formula-or-value convention.
    /// </summary>
    protected void RestoreSnapshot() => Restore(snapshot);

    /// <summary>
    /// Writes the captured (value/formula/format) state of <paramref name="cells"/> back into the sheet.
    /// </summary>
    protected void Restore(IReadOnlyDictionary<CellRef, (object? value, string? formula, Format? format)> cells)
    {
        ArgumentNullException.ThrowIfNull(cells);

        foreach (var entry in cells)
        {
            var cellRef = entry.Key;
            var (value, formula, format) = entry.Value;
            var cell = sheet.Cells[cellRef.Row, cellRef.Column];

            if (formula is not null)
            {
                cell.Formula = formula;
            }
            else
            {
                cell.Value = value;
            }

            if (format is not null)
            {
                cell.Format = format;
            }
        }
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
