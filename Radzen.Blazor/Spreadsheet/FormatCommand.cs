using System;
using System.Collections.Generic;

using Radzen.Blazor.Spreadsheet;
namespace Radzen.Documents.Spreadsheet;

#nullable enable

/// <summary>
/// Represents a command to apply formatting to a range of cells in a spreadsheet.
/// </summary>
public class FormatCommand(Sheet sheet, RangeRef range, Format format) : ICommand
{
    private readonly Sheet sheet = sheet;
    private readonly RangeRef range = range;
    private readonly Format format = format;
    private readonly Dictionary<CellRef, Format> existing = [];

    /// <summary>
    /// Executes the command to apply the specified format to the cells in the given range.
    /// </summary>
    public bool Execute()
    {
        existing.Clear();

        foreach (var cellRef in range.GetCells())
        {
            var cell = sheet.Cells[cellRef.Row, cellRef.Column];

            existing[cellRef] = cell.Format.Clone();

            cell.Format = format.Clone();
        }
        return true;
    }

    /// <summary>
    /// Reverts the formatting changes made by this command, restoring the previous formats of the cells.
    /// </summary>
    public void Unexecute()
    {
        foreach (var kvp in existing)
        {
            if (sheet.Cells.TryGet(kvp.Key.Row, kvp.Key.Column, out var cell))
            {
                cell.Format = kvp.Value.Clone();
            }
        }
    }
} 