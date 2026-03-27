using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet;

/// <summary>
/// Command to resize a chart.
/// </summary>
public class ResizeChartCommand : ICommand
{
    private readonly SheetChart chart;
    private readonly CellAnchor? newTo;
    private readonly long newWidth;
    private readonly long newHeight;
    private CellAnchor? oldTo;
    private long oldWidth;
    private long oldHeight;

    /// <summary>
    /// Creates a resize command for a TwoCellAnchor chart.
    /// </summary>
    public ResizeChartCommand(SheetChart chart, CellAnchor newTo)
    {
        this.chart = chart;
        this.newTo = newTo;
    }

    /// <summary>
    /// Creates a resize command for a OneCellAnchor chart.
    /// </summary>
    public ResizeChartCommand(SheetChart chart, long newWidth, long newHeight)
    {
        this.chart = chart;
        this.newWidth = newWidth;
        this.newHeight = newHeight;
    }

    /// <inheritdoc/>
    public bool Execute()
    {
        if (newTo is not null)
        {
            oldTo = chart.To?.Clone();
            chart.To = newTo.Clone();
        }
        else
        {
            oldWidth = chart.Width;
            oldHeight = chart.Height;
            chart.Width = newWidth;
            chart.Height = newHeight;
        }

        return true;
    }

    /// <inheritdoc/>
    public void Unexecute()
    {
        if (newTo is not null)
        {
            chart.To = oldTo?.Clone();
        }
        else
        {
            chart.Width = oldWidth;
            chart.Height = oldHeight;
        }
    }
}
