using System;

using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet;

#nullable enable

/// <summary>
/// Command that deletes a contiguous range of rows and supports undo by re-inserting blank rows
/// and restoring the cells that were captured before deletion.
/// </summary>
public class DeleteRowsCommand : DeleteRangeCommandBase
{
    /// <inheritdoc/>
    public override SheetAction RequiredAction => SheetAction.DeleteRows;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteRowsCommand"/> class.
    /// </summary>
    /// <param name="sheet">The worksheet to modify.</param>
    /// <param name="startRowIndex">The first row index to delete (inclusive).</param>
    /// <param name="endRowIndex">The last row index to delete (inclusive).</param>
    public DeleteRowsCommand(Worksheet sheet, int startRowIndex, int endRowIndex)
        : base(sheet, startRowIndex, endRowIndex)
    {
        if (startRowIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(startRowIndex), startRowIndex, $"Start row index {startRowIndex} cannot be negative.");
        }

        if (endRowIndex < startRowIndex || endRowIndex >= sheet.RowCount
            || endRowIndex - startRowIndex + 1 >= sheet.RowCount)
        {
            throw new ArgumentOutOfRangeException(nameof(endRowIndex), endRowIndex, $"End row index {endRowIndex} is out of range for a sheet with {sheet.RowCount} rows (start was {startRowIndex}).");
        }
    }

    /// <inheritdoc/>
    protected override bool IsInsideRange(CellRef address) => address.Row >= startIndex && address.Row <= endIndex;

    /// <inheritdoc/>
    protected override bool ReferencesRange(Cell cell)
    {
        ArgumentNullException.ThrowIfNull(cell);

        var tree = cell.FormulaSyntaxTree;
        if (tree is null)
        {
            return false;
        }

        for (var row = startIndex; row <= endIndex; row++)
        {
            var r = row;
            if (tree.Find(node =>
                    node is CellSyntaxNode c && c.Token.Address.Row == r
                    || node is RangeSyntaxNode range && range.Start.Token.Address.Row <= r && range.End.Token.Address.Row >= r).Count > 0)
            {
                return true;
            }
        }

        return false;
    }

    /// <inheritdoc/>
    protected override void DeleteRange()
    {
        for (var r = endIndex; r >= startIndex; r--)
        {
            sheet.DeleteRow(r);
        }
    }

    /// <inheritdoc/>
    protected override void ReinsertRange()
    {
        sheet.InsertRow(startIndex, Count);
    }
}
