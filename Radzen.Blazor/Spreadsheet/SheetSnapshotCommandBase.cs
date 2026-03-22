using System;
using System.Collections.Generic;
using System.Linq;

using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet;

#nullable enable

/// <summary>
/// Base class for commands that need a full sheet snapshot for undo support.
/// </summary>
public abstract class SheetSnapshotCommandBase : ICommand
{
    /// <summary>
    /// The sheet being operated on.
    /// </summary>
    protected readonly Worksheet sheet;

    private readonly Dictionary<(int row, int column), (object? value, string? formula, Format? format)> backupCells;
    private readonly int originalRowCount;
    private readonly int originalColumnCount;

    /// <summary>
    /// Initializes a new instance of the <see cref="SheetSnapshotCommandBase"/> class.
    /// </summary>
    protected SheetSnapshotCommandBase(Worksheet sheet)
    {
        this.sheet = sheet ?? throw new ArgumentNullException(nameof(sheet));

        originalRowCount = sheet.RowCount;
        originalColumnCount = sheet.ColumnCount;

        backupCells = [];

        foreach (var cell in sheet.Cells.GetPopulatedCells())
        {
            backupCells[(cell.Address.Row, cell.Address.Column)] = (cell.Value, cell.Formula, cell.Format?.Clone());
        }
    }

    /// <summary>
    /// Executes the command.
    /// </summary>
    public bool Execute()
    {
        DoExecute();
        return true;
    }

    /// <summary>
    /// Unexecutes the command by restoring the sheet to its original state.
    /// </summary>
    public void Unexecute()
    {
        while (sheet.RowCount > originalRowCount)
        {
            sheet.DeleteRow(sheet.RowCount - 1);
        }
        while (sheet.RowCount < originalRowCount)
        {
            sheet.InsertRow(sheet.RowCount, 1);
        }

        while (sheet.ColumnCount > originalColumnCount)
        {
            sheet.DeleteColumn(sheet.ColumnCount - 1);
        }
        while (sheet.ColumnCount < originalColumnCount)
        {
            sheet.InsertColumn(sheet.ColumnCount, 1);
        }

        // Clear any cells that were created by the operation but weren't in the backup
        foreach (var cell in sheet.Cells.GetPopulatedCells().ToList())
        {
            if (!backupCells.ContainsKey((cell.Address.Row, cell.Address.Column)))
            {
                cell.Value = null;
                cell.Formula = null;
            }
        }

        // Restore backed-up cells
        foreach (var ((row, column), (value, formula, format)) in backupCells)
        {
            var cell = sheet.Cells[row, column];

            if (formula != null)
            {
                cell.Formula = formula;
            }
            else
            {
                cell.Value = value;
            }

            if (format != null)
            {
                cell.Format = format;
            }
        }
    }

    /// <summary>
    /// Executes the command.
    /// </summary>
    protected abstract void DoExecute();
}
