using System;

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

    private readonly object?[,] backupValues;
    private readonly string?[,] backupFormulas;
    private readonly Format?[,] backupFormats;
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

        backupValues = new object?[sheet.RowCount, sheet.ColumnCount];
        backupFormulas = new string?[sheet.RowCount, sheet.ColumnCount];
        backupFormats = new Format?[sheet.RowCount, sheet.ColumnCount];

        for (var r = 0; r < sheet.RowCount; r++)
        {
            for (var c = 0; c < sheet.ColumnCount; c++)
            {
                var cell = sheet.Cells[r, c];
                backupValues[r, c] = cell.Value;
                backupFormulas[r, c] = cell.Formula;
                backupFormats[r, c] = cell.Format?.Clone();
            }
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

        for (var r = 0; r < originalRowCount; r++)
        {
            for (var c = 0; c < originalColumnCount; c++)
            {
                var cell = sheet.Cells[r, c];
                cell.Value = backupValues[r, c];
                cell.Formula = backupFormulas[r, c];
                if (backupFormats[r, c] != null)
                {
                    cell.Format = backupFormats[r, c]!;
                }
            }
        }
    }

    /// <summary>
    /// Executes the command.
    /// </summary>
    protected abstract void DoExecute();
}
