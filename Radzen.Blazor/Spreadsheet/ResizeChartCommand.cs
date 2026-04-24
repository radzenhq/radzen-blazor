using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet;

/// <summary>
/// Command to resize a chart.
/// </summary>
public class ResizeChartCommand : ICommand
{
    private readonly SheetChart chart;
    private readonly CellAnchor? newFrom;
    private readonly CellAnchor? newTo;
    private readonly double newWidth;
    private readonly double newHeight;
    private CellAnchor? oldFrom;
    private CellAnchor? oldTo;
    private double oldWidth;
    private double oldHeight;

    /// <summary>
    /// Creates a resize command for a TwoCellAnchor chart.
    /// </summary>
    public ResizeChartCommand(SheetChart chart, CellAnchor newTo)
    {
        this.chart = chart;
        this.newTo = newTo;
    }

    /// <summary>
    /// Creates a resize command for a OneCellAnchor chart. Optionally moves the
    /// From anchor when resizing from the left or top edge.
    /// </summary>
    public ResizeChartCommand(SheetChart chart, double newWidth, double newHeight, CellAnchor? newFrom = null)
    {
        this.chart = chart;
        this.newWidth = newWidth;
        this.newHeight = newHeight;
        this.newFrom = newFrom;
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

            if (newFrom is not null)
            {
                oldFrom = chart.From.Clone();
                chart.From = newFrom.Clone();
            }
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

            if (oldFrom is not null)
            {
                chart.From = oldFrom.Clone();
            }
        }
    }
}
