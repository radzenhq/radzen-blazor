using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
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
    /// Gets or sets the text being edited. Editing started with F2 or a double-click begins with the
    /// cell's value as shown in the formula bar; editing started by typing begins with the typed text.
    /// Update it as the user types: when the user moves to another cell, the spreadsheet commits this text.
    /// </summary>
    public string? Value
    {
        get => editor.Value;
        set => editor.Value = value;
    }

    /// <summary>
    /// Commits the edited value to the cell. The value is converted to a string with the workbook
    /// culture (the same culture the commit re-parses with) and applied through the undo/redo system.
    /// </summary>
    public async Task CommitAsync(object? value)
    {
        editor.Value = value is IFormattable formattable
            ? formattable.ToString(null, Worksheet.Culture)
            : value?.ToString();

        await spreadsheet.AcceptAsync();

        if (spreadsheet is RadzenSpreadsheet radzenSpreadsheet)
        {
            await radzenSpreadsheet.Element.FocusAsync();
        }
    }

    /// <summary>
    /// Cancels the current edit operation.
    /// </summary>
    public void Cancel() => editor.Cancel();
}
