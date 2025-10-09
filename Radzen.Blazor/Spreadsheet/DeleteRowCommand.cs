using System;

namespace Radzen.Blazor.Spreadsheet;

#nullable enable

/// <summary>
/// Command that deletes a single row and supports undo by snapshotting sheet state.
/// </summary>
public class DeleteRowCommand : ICommand
{
    private readonly Sheet sheet;
    private readonly int rowIndex;

    private readonly object?[,] backupValues;
    private readonly string?[,] backupFormulas;
    private readonly Format?[,] backupFormats;
    private readonly int originalRowCount;
    private readonly int originalColumnCount;

    /// <summary>
    /// Creates a new <see cref="DeleteRowCommand"/>.
    /// </summary>
    /// <param name="sheet"></param>
    /// <param name="rowIndex"></param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public DeleteRowCommand(Sheet sheet, int rowIndex)
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
        sheet.DeleteRow(rowIndex);
        return true;
    }

    /// <summary>
    /// Undoes the command.
    /// </summary>

    public void Unexecute()
    {
        if (sheet.RowCount < originalRowCount)
        {
            var toAdd = originalRowCount - sheet.RowCount;
            sheet.InsertRow(sheet.RowCount, toAdd);
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
}