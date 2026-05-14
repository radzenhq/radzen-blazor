#nullable enable

using Radzen.Documents.Spreadsheet;
using System.Collections.Generic;
namespace Radzen.Blazor.Spreadsheet;

class ClearContentsCommand(Worksheet sheet, RangeRef range) : ICommand, IProtectedCommand
{
    /// <inheritdoc/>
    public SheetAction RequiredAction => SheetAction.EditCell;

    private readonly Dictionary<CellRef, (object? value, string? formula)> snapshot = [];

    public bool Execute()
    {
        snapshot.Clear();

        for (var row = range.Start.Row; row <= range.End.Row; row++)
        {
            for (var column = range.Start.Column; column <= range.End.Column; column++)
            {
                if (sheet.Cells.TryGet(row, column, out var cell))
                {
                    var cellRef = new CellRef(row, column);
                    snapshot[cellRef] = (cell.Value, cell.Formula);

                    cell.Formula = null;
                    cell.Value = null;
                }
            }
        }

        return snapshot.Count > 0;
    }

    public void Unexecute()
    {
        foreach (var (cellRef, (value, formula)) in snapshot)
        {
            if (sheet.Cells.TryGet(cellRef.Row, cellRef.Column, out var cell))
            {
                if (formula is not null)
                {
                    cell.Formula = formula;
                }
                else
                {
                    cell.Value = value;
                }
            }
        }
    }
}
