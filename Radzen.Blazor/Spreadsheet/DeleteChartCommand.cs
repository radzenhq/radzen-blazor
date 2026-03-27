using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet;

/// <summary>
/// Command to delete a chart from a sheet.
/// </summary>
public class DeleteChartCommand(Worksheet sheet, SheetChart chart) : ICommand
{
    private readonly Worksheet sheet = sheet;
    private readonly SheetChart chart = chart;

    /// <inheritdoc/>
    public bool Execute()
    {
        sheet.RemoveChart(chart);

        if (sheet.SelectedChart == chart)
        {
            sheet.SelectedChart = null;
        }

        return true;
    }

    /// <inheritdoc/>
    public void Unexecute()
    {
        sheet.AddChart(chart);
    }
}
