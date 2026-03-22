#nullable enable

using System.Collections.Generic;

using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet;

class ClearContentsCommand(Worksheet sheet, RangeRef range) : ICommand
{
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
