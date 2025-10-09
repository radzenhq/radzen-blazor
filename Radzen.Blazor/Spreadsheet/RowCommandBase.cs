using System;

namespace Radzen.Blazor.Spreadsheet;

#nullable enable

/// <summary>
/// Base class for row commands providing snapshot/restore and undo/redo support.
/// </summary>
public abstract class RowCommandBase : ICommand
{
    /// <summary>
    /// The sheet being operated on.
    /// </summary>
    protected readonly Sheet sheet;
    /// <summary>
    /// The row index being operated on.
    /// </summary>
    protected readonly int rowIndex;

    private readonly object?[,] backupValues;
    private readonly string?[,] backupFormulas;
    private readonly Format?[,] backupFormats;
    private readonly int originalRowCount;
    private readonly int originalColumnCount;

    /// <summary>
    /// Initializes a new instance of the <see cref="RowCommandBase"/> class.
    /// </summary>
    /// <param name="sheet">The sheet being operated on.</param>
    /// <param name="rowIndex">The row index being operated on.</param>
    protected RowCommandBase(Sheet sheet, int rowIndex)
    {
        this.sheet = sheet ?? throw new ArgumentNullException(nameof(sheet));
        if (rowIndex < 0 || rowIndex >= sheet.RowCount)
        {
            throw new ArgumentOutOfRangeException(nameof(rowIndex));
        }

        this.rowIndex = rowIndex;

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
    /// <returns></returns>
    public bool Execute()
    {
        DoExecute();
        return true;
    }

    /// <summary>
    /// Unexecutes the command.
    /// </summary>
    public void Unexecute()
    {
        // Restore row count
        while (sheet.RowCount > originalRowCount)
        {
            sheet.DeleteRow(sheet.RowCount - 1);
        }
        while (sheet.RowCount < originalRowCount)
        {
            sheet.InsertRow(sheet.RowCount, 1);
        }

        // Columns should not change for row commands, but guard for consistency
        while (sheet.ColumnCount > originalColumnCount)
        {
            sheet.DeleteColumn(sheet.ColumnCount - 1);
        }
        while (sheet.ColumnCount < originalColumnCount)
        {
            sheet.InsertColumn(sheet.ColumnCount, 1);
        }

        // Restore cell contents
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