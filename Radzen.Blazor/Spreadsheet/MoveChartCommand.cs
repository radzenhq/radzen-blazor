using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet;

/// <summary>
/// Command to move a chart to new anchor positions.
/// </summary>
public class MoveChartCommand(SheetChart chart, CellAnchor newFrom, CellAnchor? newTo) : ICommand
{
    private readonly SheetChart chart = chart;
    private readonly CellAnchor newFrom = newFrom;
    private readonly CellAnchor? newTo = newTo;
    private CellAnchor? oldFrom;
    private CellAnchor? oldTo;

    /// <inheritdoc/>
    public bool Execute()
    {
        oldFrom = chart.From.Clone();
        oldTo = chart.To?.Clone();

        chart.From = newFrom.Clone();

        if (newTo is not null)
        {
            chart.To = newTo.Clone();
        }

        return true;
    }

    /// <inheritdoc/>
    public void Unexecute()
    {
        if (oldFrom is not null)
        {
            chart.From = oldFrom.Clone();
        }

        if (oldTo is not null)
        {
            chart.To = oldTo.Clone();
        }
    }
}
