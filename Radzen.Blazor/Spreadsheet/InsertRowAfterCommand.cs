using System;

namespace Radzen.Blazor.Spreadsheet;

/// <summary>
/// Command that inserts a single row AFTER the specified row index and supports undo via snapshot.
/// </summary>
/// <remarks>
/// Creates a new <see cref="InsertRowAfterCommand"/>.
/// </remarks>
/// <param name="sheet"></param>
/// <param name="rowIndex"></param>
public class InsertRowAfterCommand(Sheet sheet, int rowIndex) : RowCommandBase(sheet, Math.Max(0, Math.Min(sheet.RowCount - 1, rowIndex)))
{
    private readonly int baseRowIndex = rowIndex;

    /// <summary>
    /// Executes the command.
    /// </summary>
    /// <returns></returns>
    protected override void DoExecute() => sheet.InsertRow(baseRowIndex + 1, 1);
}