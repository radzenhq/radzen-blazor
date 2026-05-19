using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet;

/// <summary>
/// Command to insert a chart into a sheet.
/// </summary>
public class InsertChartCommand(Worksheet sheet, SheetChart chart) : ICommand
{
    /// <inheritdoc/>
    public SpreadsheetFeature? Feature => SpreadsheetFeature.Charts;

    private readonly Worksheet sheet = sheet;
    private readonly SheetChart chart = chart;

    /// <inheritdoc/>
    public bool Execute()
    {
        sheet.AddChart(chart);
        return true;
    }

    /// <inheritdoc/>
    public void Unexecute()
    {
        sheet.RemoveChart(chart);
    }
}
