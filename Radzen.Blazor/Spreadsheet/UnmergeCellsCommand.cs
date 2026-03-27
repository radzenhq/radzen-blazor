using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet;

#nullable enable

/// <summary>
/// Command to unmerge cells that contain a given cell address.
/// </summary>
public class UnmergeCellsCommand(Worksheet sheet, CellRef address) : ICommand, IProtectedCommand
{
    /// <inheritdoc/>
    public SheetAction RequiredAction => SheetAction.FormatCells;

    private readonly Worksheet sheet = sheet;
    private readonly CellRef address = address;
    private RangeRef removedRange = RangeRef.Invalid;

    /// <inheritdoc/>
    public bool Execute()
    {
        removedRange = sheet.MergedCells.GetMergedRange(address);

        if (removedRange == RangeRef.Invalid)
        {
            return false;
        }

        sheet.MergedCells.Remove(removedRange);
        return true;
    }

    /// <inheritdoc/>
    public void Unexecute()
    {
        if (removedRange != RangeRef.Invalid)
        {
            sheet.MergedCells.Add(removedRange);
        }
    }
}
