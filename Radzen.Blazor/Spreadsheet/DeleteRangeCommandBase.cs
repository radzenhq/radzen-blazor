using System;
using System.Collections.Generic;
using System.Linq;

using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet;

#nullable enable

/// <summary>
/// Base class for row and column delete commands. Captures only the cells that will be removed
/// or whose formulas reference the deleted range, then performs the delete. Undo re-inserts
/// the blank rows/columns and restores the captured cells in place.
/// </summary>
public abstract class DeleteRangeCommandBase : ICommand, IProtectedCommand
{
    /// <inheritdoc/>
    public SpreadsheetFeature? Feature => SpreadsheetFeature.Editing;

    /// <inheritdoc/>
    public abstract SheetAction RequiredAction { get; }

    /// <summary>
    /// The worksheet being operated on.
    /// </summary>
    protected readonly Worksheet sheet;

    /// <summary>
    /// The first index in the range to delete (inclusive).
    /// </summary>
    protected readonly int startIndex;

    /// <summary>
    /// The last index in the range to delete (inclusive).
    /// </summary>
    protected readonly int endIndex;

    private readonly Dictionary<CellRef, (object? value, string? formula, Format? format)> snapshot = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteRangeCommandBase"/> class.
    /// </summary>
    /// <param name="sheet">The worksheet to modify.</param>
    /// <param name="startIndex">The first row/column index to delete (inclusive).</param>
    /// <param name="endIndex">The last row/column index to delete (inclusive).</param>
    protected DeleteRangeCommandBase(Worksheet sheet, int startIndex, int endIndex)
    {
        this.sheet = sheet ?? throw new ArgumentNullException(nameof(sheet));
        this.startIndex = startIndex;
        this.endIndex = endIndex;
    }

    /// <summary>
    /// Gets the number of indices in the deletion range.
    /// </summary>
    protected int Count => endIndex - startIndex + 1;

    /// <inheritdoc/>
    public bool Execute()
    {
        snapshot.Clear();
        CaptureAffectedCells();
        DeleteRange();
        return true;
    }

    /// <inheritdoc/>
    public void Unexecute()
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
                snapshot[cell.Address] = (cell.Value, cell.Formula, cell.Format?.Clone());
            }
        }
    }

    private void RestoreSnapshot()
    {
        foreach (var entry in snapshot)
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
}
