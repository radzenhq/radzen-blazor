using System;

using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet;

#nullable enable

/// <summary>
/// Command that deletes a contiguous range of columns and supports undo by re-inserting blank
/// columns and restoring the cells that were captured before deletion.
/// </summary>
public class DeleteColumnsCommand : DeleteRangeCommandBase
{
    /// <inheritdoc/>
    public override SheetAction RequiredAction => SheetAction.DeleteColumns;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteColumnsCommand"/> class.
    /// </summary>
    /// <param name="sheet">The worksheet to modify.</param>
    /// <param name="startColumnIndex">The first column index to delete (inclusive).</param>
    /// <param name="endColumnIndex">The last column index to delete (inclusive).</param>
    public DeleteColumnsCommand(Worksheet sheet, int startColumnIndex, int endColumnIndex)
        : base(sheet, startColumnIndex, endColumnIndex)
    {
        if (startColumnIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(startColumnIndex), startColumnIndex, $"Start column index {startColumnIndex} cannot be negative.");
        }

        if (endColumnIndex < startColumnIndex || endColumnIndex >= sheet.ColumnCount
            || endColumnIndex - startColumnIndex + 1 >= sheet.ColumnCount)
        {
            throw new ArgumentOutOfRangeException(nameof(endColumnIndex), endColumnIndex, $"End column index {endColumnIndex} is out of range for a sheet with {sheet.ColumnCount} columns (start was {startColumnIndex}).");
        }
    }

    /// <inheritdoc/>
    protected override bool IsInsideRange(CellRef address) => address.Column >= startIndex && address.Column <= endIndex;

    /// <inheritdoc/>
    protected override bool ReferencesRange(Cell cell)
    {
        ArgumentNullException.ThrowIfNull(cell);

        var tree = cell.FormulaSyntaxTree;
        if (tree is null)
        {
            return false;
        }

        for (var col = startIndex; col <= endIndex; col++)
        {
            var c = col;
            if (tree.Find(node =>
                    node is CellSyntaxNode cellNode && cellNode.Token.Address.Column == c
                    || node is RangeSyntaxNode range && range.Start.Token.Address.Column <= c && range.End.Token.Address.Column >= c).Count > 0)
            {
                return true;
            }
        }

        return false;
    }

    /// <inheritdoc/>
    protected override void DeleteRange()
    {
        for (var c = endIndex; c >= startIndex; c--)
        {
            sheet.DeleteColumn(c);
        }
    }

    /// <inheritdoc/>
    protected override void ReinsertRange()
    {
        sheet.InsertColumn(startIndex, Count);
    }
}
