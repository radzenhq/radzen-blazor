using Xunit;

using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet.Tests;

/// <summary>
/// Guards that bulk cell operations evaluate dependent formulas once after the batch,
/// not once per written cell. Without batching, sorting a range that feeds an aggregate
/// (e.g. SUM) re-evaluates the whole formula graph per write - O(N^2) per sort.
/// </summary>
public class SortRecalcBatchingTests
{
    [Fact]
    public void Sort_EvaluatesAggregateOnce_NotPerWrittenCell()
    {
        var sheet = new Worksheet(20, 5);

        for (var r = 0; r < 8; r++)
        {
            sheet.Cells[r, 0].Value = (double)(8 - r); // A1..A8 = 8,7,...,1
            sheet.Cells[r, 1].Formula = $"=A{r + 1}*2"; // B = A*2
        }

        // Aggregate over the sorted range, placed OUTSIDE it so its own value isn't
        // rewritten by the sort - every Changed we see is a re-evaluation.
        sheet.Cells[10, 3].Formula = "=SUM(B1:B8)"; // D11

        var aggregateEvaluations = 0;
        sheet.Cells[10, 3].Changed += _ => aggregateEvaluations++;

        sheet.Sort(new RangeRef(new CellRef(0, 0), new CellRef(7, 1)), SortOrder.Ascending, 0);

        // Batched: a single recalc after the writes. Pre-fix this was ~24 (once per
        // value/formula write that re-triggered a whole-graph recalc).
        Assert.True(aggregateEvaluations <= 2, $"aggregate re-evaluated {aggregateEvaluations} times; expected <= 2 (batched)");
    }

    [Fact]
    public void Sort_StillReordersAndKeepsAggregateCorrect()
    {
        var sheet = new Worksheet(20, 5);

        for (var r = 0; r < 8; r++)
        {
            sheet.Cells[r, 0].Value = (double)(8 - r);
            sheet.Cells[r, 1].Value = (double)(8 - r);
        }

        sheet.Cells[10, 3].Formula = "=SUM(A1:A8)";

        sheet.Sort(new RangeRef(new CellRef(0, 0), new CellRef(7, 1)), SortOrder.Ascending, 0);

        Assert.Equal(1d, sheet.Cells[0, 0].Value);
        Assert.Equal(8d, sheet.Cells[7, 0].Value);
        Assert.Equal(36d, sheet.Cells[10, 3].Value); // 1+2+...+8, unchanged by reorder
    }
}
