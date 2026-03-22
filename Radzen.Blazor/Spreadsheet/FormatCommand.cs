using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet;

#nullable enable

/// <summary>
/// Represents a command to apply formatting to a range of cells in a spreadsheet.
/// </summary>
public class FormatCommand(Worksheet sheet, RangeRef range, Format format)
    : RangeFormatCommandBase(sheet, range)
{
    /// <inheritdoc/>
    protected override Format ApplyFormat(Format current, CellRef cellRef) => format.Clone();
}
