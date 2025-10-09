using System;

namespace Radzen.Blazor.Spreadsheet;

#nullable enable

/// <summary>
/// Command that inserts a single row AFTER the specified row index and supports undo via snapshot.
/// </summary>
public class InsertRowAfterCommand : ICommand
{
    private readonly Sheet sheet;
    private readonly int baseRowIndex;

    private readonly object?[,] backupValues;
    private readonly string?[,] backupFormulas;
    private readonly Format?[,] backupFormats;
    private readonly int originalRowCount;
    private readonly int originalColumnCount;

    /// <summary>
    /// Creates a new <see cref="InsertRowAfterCommand"/>.
    /// </summary>
    /// <param name="sheet"></param>
    /// <param name="rowIndex"></param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public InsertRowAfterCommand(Sheet sheet, int rowIndex)
    {
        this.sheet = sheet ?? throw new ArgumentNullException(nameof(sheet));

        if (rowIndex < -1 || rowIndex >= sheet.RowCount)
        {
            throw new ArgumentOutOfRangeException(nameof(rowIndex));
        }

        baseRowIndex = rowIndex;

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
        var insertIndex = baseRowIndex + 1;
        sheet.InsertRow(insertIndex, 1);
        return true;
    }

    /// <summary>
    /// Undoes the command.
    /// </summary>
    public void Unexecute()
    {
        // Restore row/column counts
        while (sheet.RowCount > originalRowCount)
        {
            sheet.DeleteRow(sheet.RowCount - 1);
        }
        while (sheet.RowCount < originalRowCount)
        {
            sheet.InsertRow(sheet.RowCount, 1);
        }

        // Columns should not change for this command, but guard for consistency
        while (sheet.ColumnCount > originalColumnCount)
        {
            sheet.DeleteColumn(sheet.ColumnCount - 1);
        }
        while (sheet.ColumnCount < originalColumnCount)
        {
            sheet.InsertColumn(sheet.ColumnCount, 1);
        }

        // Restore all cells
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
}


