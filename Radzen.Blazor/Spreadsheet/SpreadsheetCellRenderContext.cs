using System;
using System.Threading.Tasks;
using Radzen.Documents.Spreadsheet;

namespace Radzen.Blazor.Spreadsheet;

#nullable enable

/// <summary>
/// Provides context for custom cell renderers in a spreadsheet.
/// </summary>
public class SpreadsheetCellRenderContext
{
    /// <summary>
    /// Gets the formatted display value of the cell.
    /// </summary>
    public string? FormattedValue { get; }

    /// <summary>
    /// Gets the cell.
    /// </summary>
    public Cell Cell { get; }

    /// <summary>
    /// Gets the worksheet that contains the cell.
    /// </summary>
    public Worksheet Worksheet { get; }

    internal SpreadsheetCellRenderContext(string? formattedValue, Cell cell, Worksheet worksheet)
    {
        FormattedValue = formattedValue;
        Cell = cell;
        Worksheet = worksheet;
    }
}

/// <summary>
/// Provides context for custom cell editors in a spreadsheet.
/// </summary>
public class SpreadsheetCellEditContext : SpreadsheetCellRenderContext
{
    private readonly Editor editor;
    private readonly ISpreadsheet spreadsheet;

    internal SpreadsheetCellEditContext(string? formattedValue, Cell cell, Worksheet worksheet, Editor editor, ISpreadsheet spreadsheet)
        : base(formattedValue, cell, worksheet)
    {
        this.editor = editor;
        this.spreadsheet = spreadsheet;
    }

    /// <summary>
    /// Commits the edited value to the cell. The value is converted to a string and applied through the undo/redo system.
    /// </summary>
    public Task CommitAsync(object? value)
    {
        editor.Value = value?.ToString();
        return spreadsheet.AcceptAsync();
    }

    /// <summary>
    /// Cancels the current edit operation.
    /// </summary>
    public void Cancel() => editor.Cancel();
}
