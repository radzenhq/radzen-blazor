using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet;

#nullable enable

/// <summary>
/// Command to set or remove a hyperlink on a cell.
/// </summary>
public class HyperlinkCommand(Worksheet sheet, CellRef address, Hyperlink? hyperlink) : ICommand, IProtectedCommand
{
    /// <inheritdoc/>
    public SheetAction RequiredAction => SheetAction.InsertHyperlinks;

    /// <inheritdoc/>
    public SpreadsheetFeature? Feature => SpreadsheetFeature.Hyperlinks;

    private readonly Worksheet sheet = sheet;
    private readonly CellRef address = address;
    private readonly Hyperlink? hyperlink = hyperlink;
    private Cell? previousCell;

    /// <inheritdoc/>
    public bool Execute()
    {
        var cell = sheet.Cells[address.Row, address.Column];

        previousCell = cell.Clone();

        cell.Hyperlink = hyperlink?.Clone();

        if (hyperlink is not null && !string.IsNullOrEmpty(hyperlink.Text))
        {
            cell.Value = hyperlink.Text;
        }
        else if (hyperlink is not null && (cell.Value is null || string.IsNullOrEmpty(cell.Value.ToString())))
        {
            cell.Value = hyperlink.Url;
        }

        return true;
    }

    /// <inheritdoc/>
    public void Unexecute()
    {
        if (previousCell is not null && sheet.Cells.TryGet(address.Row, address.Column, out var cell))
        {
            cell.CopyFrom(previousCell.Clone());
        }
    }
}
