using System;
using Xunit;

using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet.Tests;

/// <summary>
/// Guards the nesting + exception semantics of BeginUpdate/EndUpdate/Batch: formula
/// evaluation is suspended until the outermost update closes, and runs exactly once then.
/// </summary>
public class BatchNestingTests
{
    private static Worksheet SheetWithAggregate(out Func<int> evalCount)
    {
        var sheet = new Worksheet(20, 5);
        for (var r = 0; r < 8; r++)
        {
            sheet.Cells[r, 0].Value = (double)(r + 1); // A1..A8 = 1..8
        }
        sheet.Cells[10, 3].Formula = "=SUM(A1:A8)"; // D11, outside any edited range

        var count = 0;
        sheet.Cells[10, 3].Changed += _ => count++;
        evalCount = () => count;
        return sheet;
    }

    [Fact]
    public void NestedBatch_EvaluatesAggregateOnce()
    {
        var sheet = SheetWithAggregate(out var evals);

        sheet.Batch(() =>
        {
            sheet.Batch(() =>
            {
                for (var r = 0; r < 8; r++)
                {
                    sheet.Cells[r, 0].Value = (double)(r + 1) * 10;
                }
            });

            Assert.Equal(0, evals()); // inner EndUpdate must NOT recalc while outer is open
        });

        Assert.Equal(1, evals());
        Assert.Equal(360d, sheet.Cells[10, 3].Value); // (1..8)*10
    }

    [Fact]
    public void NestedBeginEndUpdate_RecalcsOnlyAtOutermost()
    {
        var sheet = SheetWithAggregate(out var evals);

        sheet.BeginUpdate();
        sheet.BeginUpdate();
        sheet.Cells[0, 0].Value = 100d;
        sheet.EndUpdate();
        Assert.Equal(0, evals());
        sheet.EndUpdate();
        Assert.Equal(1, evals());
    }

    [Fact]
    public void Batch_RecalcsAndUnwinds_EvenIfActionThrows()
    {
        var sheet = SheetWithAggregate(out var evals);

        Assert.Throws<InvalidOperationException>(() =>
            sheet.Batch(() =>
            {
                sheet.Cells[0, 0].Value = 100d;
                throw new InvalidOperationException("boom");
            }));

        Assert.Equal(1, evals()); // finally still flushed the recalc

        // Depth must have unwound to 0: a subsequent direct edit recalcs immediately.
        sheet.Cells[1, 0].Value = 200d;
        Assert.Equal(2, evals());
    }

    [Fact]
    public void EndUpdate_WithoutBegin_IsNoOpAndDoesNotCorruptDepth()
    {
        var sheet = SheetWithAggregate(out var evals);

        sheet.EndUpdate(); // unmatched - must be harmless

        sheet.Cells[0, 0].Value = 100d; // still recalcs (depth genuinely 0)
        Assert.Equal(1, evals());
    }
}
