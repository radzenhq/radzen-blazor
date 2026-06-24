using System;
using System.Collections.Generic;
using System.Linq;

using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet;

/// <summary>
/// Command that inserts a single row at the specified index. Undo deletes the inserted row
/// and restores the original formulas of any cells whose references were shifted by Execute —
/// Worksheet.DeleteRow only invalidates references pointing at the deleted (empty) row, so
/// it cannot reverse the shift on its own.
/// </summary>
public class InsertRowCommand : ICommand, IProtectedCommand
{
    /// <inheritdoc/>
    public SpreadsheetFeature? Feature => SpreadsheetFeature.Editing;

    /// <inheritdoc/>
    public SheetAction RequiredAction => SheetAction.InsertRows;

    private readonly Worksheet sheet;
    private readonly int rowIndex;
    private readonly Dictionary<CellRef, string> formulaSnapshot = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="InsertRowCommand"/> class.
    /// </summary>
    /// <param name="sheet">The worksheet to modify.</param>
    /// <param name="rowIndex">The 0-based index at which to insert the new row. Pass <c>index + 1</c> for "insert after" semantics.</param>
    public InsertRowCommand(Worksheet sheet, int rowIndex)
    {
        ArgumentNullException.ThrowIfNull(sheet);

        if (rowIndex < 0 || rowIndex > sheet.RowCount)
        {
            throw new ArgumentOutOfRangeException(nameof(rowIndex), rowIndex, $"Row index {rowIndex} is out of range for a sheet with {sheet.RowCount} rows.");
        }

        this.sheet = sheet;
        this.rowIndex = rowIndex;
    }

    /// <inheritdoc/>
    public bool Execute()
    {
        formulaSnapshot.Clear();
        foreach (var cell in sheet.Cells.GetPopulatedCells().ToList())
        {
            if (!string.IsNullOrEmpty(cell.Formula))
            {
                formulaSnapshot[cell.Address] = cell.Formula!;
            }
        }

        sheet.InsertRow(rowIndex, 1);
        return true;
    }

    /// <inheritdoc/>
    public void Unexecute()
    {
        sheet.DeleteRow(rowIndex);

        foreach (var entry in formulaSnapshot)
        {
            var address = entry.Key;
            sheet.Cells[address.Row, address.Column].Formula = entry.Value;
        }

        formulaSnapshot.Clear();
    }
}
