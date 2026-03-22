using Radzen.Blazor.Spreadsheet;
namespace Radzen.Documents.Spreadsheet;

#nullable enable

/// <summary>
/// Command to unmerge cells that contain a given cell address.
/// </summary>
public class UnmergeCellsCommand(Sheet sheet, CellRef address) : ICommand
{
    private readonly Sheet sheet = sheet;
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
