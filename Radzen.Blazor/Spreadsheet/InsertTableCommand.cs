using Radzen.Documents.Spreadsheet;

namespace Radzen.Blazor.Spreadsheet;

#nullable enable

/// <summary>
/// Adds a <see cref="Table"/> over the given range. Undo removes the table without
/// touching cell content.
/// </summary>
public class InsertTableCommand(Worksheet sheet, string name, RangeRef range, bool hasHeaders = true) : ICommand
{
    private readonly Worksheet sheet = sheet;
    private readonly string name = name;
    private readonly RangeRef range = range;
    private readonly bool hasHeaders = hasHeaders;
    private Table? created;

    /// <inheritdoc/>
    public bool Execute()
    {
        created = sheet.AddTable(name, range, hasHeaders);
        return true;
    }

    /// <inheritdoc/>
    public void Unexecute()
    {
        if (created is not null)
        {
            sheet.RemoveTable(created);
            created = null;
        }
    }
}

/// <summary>
/// Removes a <see cref="Table"/> from a sheet. Cell content is preserved (matches
/// Excel's "Convert to Range" behavior). Undo re-adds the table at its original
/// range with its original metadata.
/// </summary>
public class RemoveTableCommand(Worksheet sheet, Table table) : ICommand
{
    private readonly Worksheet sheet = sheet;
    private readonly Table table = table;
    private bool removed;

    /// <inheritdoc/>
    public bool Execute()
    {
        removed = sheet.RemoveTable(table);
        return removed;
    }

    /// <inheritdoc/>
    public void Unexecute()
    {
        if (removed)
        {
            // Re-create the table at the same range. Most metadata is reattached because
            // the original Table instance still holds it; we restore by adding the same
            // instance (sheet just stores it in the list).
            // The simplest and safe thing is to add a fresh table with the same
            // identity; downstream callers can re-apply style/totals if needed.
            var restored = sheet.AddTable(table.Name, table.Range, table.ShowHeaderRow);
            restored.DisplayName = table.DisplayName;
            restored.ShowTotalsRow = table.ShowTotalsRow;
            restored.ShowFilterButton = table.ShowFilterButton;
            restored.ShowBandedRows = table.ShowBandedRows;
            restored.ShowBandedColumns = table.ShowBandedColumns;
            restored.HighlightFirstColumn = table.HighlightFirstColumn;
            restored.HighlightLastColumn = table.HighlightLastColumn;
            restored.TableStyle = table.TableStyle;
            removed = false;
        }
    }
}
