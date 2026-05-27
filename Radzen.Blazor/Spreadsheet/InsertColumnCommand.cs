using System;
using System.Collections.Generic;
using System.Linq;

using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet;

/// <summary>
/// Command that inserts a single column at the specified index. Undo deletes the inserted column
/// and restores the original formulas of any cells whose references were shifted by Execute —
/// Worksheet.DeleteColumn only invalidates references pointing at the deleted (empty) column, so
/// it cannot reverse the shift on its own.
/// </summary>
public class InsertColumnCommand : ICommand, IProtectedCommand
{
    /// <inheritdoc/>
    public SpreadsheetFeature? Feature => SpreadsheetFeature.Editing;

    /// <inheritdoc/>
    public SheetAction RequiredAction => SheetAction.InsertColumns;

    private readonly Worksheet sheet;
    private readonly int columnIndex;
    private readonly Dictionary<CellRef, string> formulaSnapshot = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="InsertColumnCommand"/> class.
    /// </summary>
    /// <param name="sheet">The worksheet to modify.</param>
    /// <param name="columnIndex">The 0-based index at which to insert the new column. Pass <c>index + 1</c> for "insert after" semantics.</param>
    public InsertColumnCommand(Worksheet sheet, int columnIndex)
    {
        ArgumentNullException.ThrowIfNull(sheet);

        if (columnIndex < 0 || columnIndex > sheet.ColumnCount)
        {
            throw new ArgumentOutOfRangeException(nameof(columnIndex), columnIndex, $"Column index {columnIndex} is out of range for a sheet with {sheet.ColumnCount} columns.");
        }

        this.sheet = sheet;
        this.columnIndex = columnIndex;
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

        sheet.InsertColumn(columnIndex, 1);
        return true;
    }

    /// <inheritdoc/>
    public void Unexecute()
    {
        sheet.DeleteColumn(columnIndex);

        foreach (var entry in formulaSnapshot)
        {
            var address = entry.Key;
            sheet.Cells[address.Row, address.Column].Formula = entry.Value;
        }

        formulaSnapshot.Clear();
    }
}
