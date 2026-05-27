using System;
using System.Collections.Generic;
using Xunit;

using Radzen.Blazor.Spreadsheet;
using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet.Tests;

public class AxisIndexTests
{
    // Naive reference implementation: walk every index and sum sizes.
    private static double NaivePosition(Axis axis, int index)
    {
        if (index <= 0)
        {
            return 0;
        }

        var sum = 0d;
        var end = Math.Min(index, axis.Count);
        for (var i = 0; i < end; i++)
        {
            if (!axis.IsHidden(i))
            {
                sum += axis[i];
            }
        }
        return sum;
    }

    private static int NaiveIndexAt(Axis axis, double pixel)
    {
        if (pixel <= 0 || axis.Count == 0)
        {
            return 0;
        }

        var pos = 0d;
        for (var i = 0; i < axis.Count; i++)
        {
            if (axis.IsHidden(i))
            {
                continue;
            }

            var nextPos = pos + axis[i];
            if (pixel < nextPos)
            {
                return i;
            }
            pos = nextPos;
        }
        return axis.Count;
    }

    [Fact]
    public void GetPositionOf_AllDefault_MatchesNaive()
    {
        var axis = new Axis(20, 100);
        for (var i = 0; i <= 100; i++)
        {
            Assert.Equal(NaivePosition(axis, i), axis.GetPositionOf(i));
        }
    }

    [Fact]
    public void GetPositionOf_CustomSizes_MatchesNaive()
    {
        var axis = new Axis(15, 50);
        axis[3] = 40;
        axis[10] = 5;
        axis[25] = 100;
        axis[49] = 1;

        for (var i = 0; i <= axis.Count; i++)
        {
            Assert.Equal(NaivePosition(axis, i), axis.GetPositionOf(i));
        }
    }

    [Fact]
    public void GetPositionOf_HiddenIndices_MatchesNaive()
    {
        var axis = new Axis(10, 30);
        axis.Hide(0);
        axis.Hide(5);
        axis.Hide(6);
        axis.Hide(29);

        for (var i = 0; i <= axis.Count; i++)
        {
            Assert.Equal(NaivePosition(axis, i), axis.GetPositionOf(i));
        }
    }

    [Fact]
    public void GetPositionOf_MixedCustomAndHidden_MatchesNaive()
    {
        var axis = new Axis(12, 40);
        axis[2] = 50;
        axis[7] = 5;
        axis[20] = 100;
        axis[33] = 8;
        axis.Hide(2);   // custom + hidden
        axis.Hide(11);  // default + hidden
        axis.Hide(20);  // custom + hidden
        axis.Hide(39);

        for (var i = 0; i <= axis.Count; i++)
        {
            Assert.Equal(NaivePosition(axis, i), axis.GetPositionOf(i));
        }
    }

    [Fact]
    public void GetPositionOf_AtCount_EqualsTotal()
    {
        var axis = new Axis(20, 25);
        axis[3] = 50;
        axis.Hide(10);
        axis.Hide(11);
        axis[15] = 8;

        Assert.Equal(axis.Total, axis.GetPositionOf(axis.Count));
    }

    [Fact]
    public void GetIndexAt_AllDefault_MatchesNaive()
    {
        var axis = new Axis(20, 100);
        var pixels = new double[] { 0, 1, 19, 20, 21, 200, 999, 1999, 2000, 2001, 5000 };
        foreach (var pixel in pixels)
        {
            Assert.Equal(NaiveIndexAt(axis, pixel), axis.GetIndexAt(pixel));
        }
    }

    [Fact]
    public void GetIndexAt_CustomAndHidden_MatchesNaive()
    {
        var axis = new Axis(10, 50);
        axis[3] = 25;
        axis[8] = 5;
        axis[20] = 100;
        axis.Hide(0);
        axis.Hide(10);
        axis.Hide(11);
        axis.Hide(49);

        for (var pixel = -5d; pixel <= axis.Total + 50; pixel += 0.5)
        {
            Assert.Equal(NaiveIndexAt(axis, pixel), axis.GetIndexAt(pixel));
        }
    }

    [Fact]
    public void GetIndexAt_AtStopBoundaries_MatchesNaive()
    {
        var axis = new Axis(10, 20);
        axis[5] = 30;
        axis[10] = 50;
        axis.Hide(15);

        for (var i = 0; i <= axis.Count; i++)
        {
            var pos = axis.GetPositionOf(i);
            Assert.Equal(NaiveIndexAt(axis, pos), axis.GetIndexAt(pos));
        }
    }

    [Fact]
    public void RoundTrip_PositionToIndex_Identity()
    {
        var axis = new Axis(15, 40);
        axis[5] = 5;
        axis[10] = 80;
        axis[30] = 20;
        axis.Hide(7);
        axis.Hide(20);

        for (var i = 0; i < axis.Count; i++)
        {
            if (axis.IsHidden(i))
            {
                continue;
            }

            var pos = axis.GetPositionOf(i);
            Assert.Equal(i, axis.GetIndexAt(pos));
        }
    }

    [Fact]
    public void Total_MatchesGetPositionOfCount()
    {
        var axis = new Axis(11, 33);
        axis[1] = 22;
        axis[5] = 7;
        axis[10] = 100;
        axis.Hide(0);
        axis.Hide(5);  // hidden custom
        axis.Hide(20);
        axis.Hide(32);

        Assert.Equal(axis.Total, axis.GetPositionOf(axis.Count));
    }

    [Fact]
    public void Insert_InvalidatesIndex()
    {
        var axis = new Axis(20, 10);
        axis[5] = 50;

        var before = axis.GetPositionOf(10);
        axis.ShiftDown(3, 1);
        axis.Count = 11;
        var after = axis.GetPositionOf(11);

        // Inserting one default-sized row should add one default size to Total.
        Assert.Equal(before + 20, after);
        Assert.Equal(NaivePosition(axis, 11), after);
    }

    [Fact]
    public void Delete_InvalidatesIndex()
    {
        var axis = new Axis(15, 10);
        axis[5] = 100;
        var positionBeforeDelete = axis.GetPositionOf(8);

        axis.ShiftUp(5); // removes the custom-sized row at index 5
        axis.Count = 9;

        var positionAfterDelete = axis.GetPositionOf(7);
        Assert.Equal(NaivePosition(axis, 7), positionAfterDelete);
        // GetPositionOf(8) summed indices 0..7 with index 5 sized 100 (total 205). After
        // deleting index 5 the new axis covers indices 0..8 with all default sizes; the
        // new GetPositionOf(7) sums 0..6 (all default 15) and equals 7 * 15 = 105, which
        // is the old sum minus the removed custom contribution (100).
        Assert.Equal(positionBeforeDelete - 100, positionAfterDelete);
    }

    [Fact]
    public void CustomSize_InvalidatesIndex()
    {
        var axis = new Axis(10, 20);
        var before = axis.GetPositionOf(15);
        axis[5] = 100;
        var after = axis.GetPositionOf(15);

        Assert.NotEqual(before, after);
        Assert.Equal(NaivePosition(axis, 15), after);
    }

    [Fact]
    public void Hide_InvalidatesIndex()
    {
        var axis = new Axis(20, 30);
        var before = axis.GetPositionOf(25);
        axis.Hide(10);
        var after = axis.GetPositionOf(25);

        Assert.Equal(before - 20, after);
        Assert.Equal(NaivePosition(axis, 25), after);
    }

    [Fact]
    public void Show_InvalidatesIndex()
    {
        var axis = new Axis(20, 30);
        axis.Hide(10);
        var hiddenPos = axis.GetPositionOf(25);
        axis.Show(10);
        var visiblePos = axis.GetPositionOf(25);

        Assert.Equal(hiddenPos + 20, visiblePos);
        Assert.Equal(NaivePosition(axis, 25), visiblePos);
    }

    [Fact]
    public void ShowAll_InvalidatesIndex()
    {
        var axis = new Axis(10, 50);
        axis.Hide(5);
        axis.Hide(15);
        axis.Hide(25);
        var hiddenTotal = axis.GetPositionOf(axis.Count);

        axis.ShowAll();
        var visibleTotal = axis.GetPositionOf(axis.Count);

        Assert.Equal(hiddenTotal + 30, visibleTotal);
        Assert.Equal(NaivePosition(axis, axis.Count), visibleTotal);
    }

    [Fact]
    public void Frozen_InvalidatesIndex()
    {
        var axis = new Axis(20, 30);
        var posBefore = axis.GetPositionOf(5);
        axis.Frozen = 3;
        var posAfter = axis.GetPositionOf(5);

        // Frozen does not affect GetPositionOf semantically; just ensure the index
        // remains consistent across the mutation.
        Assert.Equal(posBefore, posAfter);
        Assert.Equal(NaivePosition(axis, 5), posAfter);
    }

    [Fact]
    public void CountChange_InvalidatesIndex()
    {
        var axis = new Axis(10, 5);
        var posBefore = axis.GetPositionOf(5);

        axis.Count = 20;
        var posAfter = axis.GetPositionOf(15);

        Assert.Equal(50, posBefore);
        Assert.Equal(150, posAfter);
        Assert.Equal(NaivePosition(axis, 15), posAfter);
    }

    [Fact]
    public void BeginUpdate_DoesNotFireChanged_UntilEndUpdate()
    {
        var axis = new Axis(10, 10);
        var changes = 0;
        axis.Changed += () => changes++;

        axis.BeginUpdate();
        axis[1] = 50;
        axis[2] = 80;
        axis.Hide(3);
        axis.Hide(4);
        Assert.Equal(0, changes);

        axis.EndUpdate();
        Assert.Equal(1, changes);

        // After EndUpdate the index must reflect all batched mutations.
        Assert.Equal(NaivePosition(axis, 6), axis.GetPositionOf(6));
        Assert.Equal(NaivePosition(axis, axis.Count), axis.GetPositionOf(axis.Count));
    }

    [Fact]
    public void BeginUpdate_QueryDuringBatchReflectsState()
    {
        var axis = new Axis(10, 20);
        axis.BeginUpdate();
        axis[5] = 100;
        // The index must still answer correctly mid-batch.
        Assert.Equal(NaivePosition(axis, 10), axis.GetPositionOf(10));

        axis.Hide(2);
        Assert.Equal(NaivePosition(axis, 10), axis.GetPositionOf(10));

        axis.EndUpdate();
        Assert.Equal(NaivePosition(axis, 10), axis.GetPositionOf(10));
    }

    [Fact]
    public void EmptyAxis_GetPositionOfAndGetIndexAt()
    {
        var axis = new Axis(15, 0);
        Assert.Equal(0, axis.GetPositionOf(0));
        Assert.Equal(0, axis.GetIndexAt(0));
        Assert.Equal(0, axis.GetIndexAt(100));
        Assert.Equal(0, axis.Total);
    }

    [Fact]
    public void AllHidden_TotalIsZero_AndIndexLookupsHandleGracefully()
    {
        var axis = new Axis(20, 5);
        for (var i = 0; i < axis.Count; i++)
        {
            axis.Hide(i);
        }

        Assert.Equal(0, axis.Total);
        Assert.Equal(0, axis.GetPositionOf(0));
        Assert.Equal(NaivePosition(axis, 5), axis.GetPositionOf(5));
    }

    [Fact]
    public void QueryBeyondLastIndex_ReturnsTotal()
    {
        var axis = new Axis(10, 5);
        axis[2] = 50;

        var total = axis.Total;
        Assert.Equal(total, axis.GetPositionOf(axis.Count));
        Assert.Equal(axis.Count, axis.GetIndexAt(total));
        Assert.Equal(axis.Count, axis.GetIndexAt(total + 100));
    }

    [Fact]
    public void QueryAtPixelZero_ReturnsZero()
    {
        var axis = new Axis(20, 30);
        axis[0] = 50;
        Assert.Equal(0, axis.GetIndexAt(0));
        Assert.Equal(0, axis.GetIndexAt(-10));
    }

    [Fact]
    public void AdjacentCustomSizedRows_QueryAtBoundary()
    {
        var axis = new Axis(10, 10);
        axis[3] = 30;
        axis[4] = 40;
        axis[5] = 50;

        // Boundary between index 3 and 4 sits at GetPositionOf(4).
        var boundary = axis.GetPositionOf(4);
        Assert.Equal(4, axis.GetIndexAt(boundary));
        Assert.Equal(3, axis.GetIndexAt(boundary - 0.5));

        // Boundary between index 4 and 5 sits at GetPositionOf(5).
        var boundary2 = axis.GetPositionOf(5);
        Assert.Equal(5, axis.GetIndexAt(boundary2));
        Assert.Equal(4, axis.GetIndexAt(boundary2 - 0.5));
    }

    [Fact]
    public void Matrix_GetPositionOf_MatchesNaive_AcrossConfigurations()
    {
        var configurations = new[]
        {
            new Action<Axis>(_ => { }),
            a => { a[3] = 50; a[7] = 5; },
            a => { a.Hide(0); a.Hide(4); a.Hide(9); },
            a => { a[2] = 30; a.Hide(2); },
            a => { a[1] = 100; a.Hide(5); a[8] = 1; a.Frozen = 2; },
        };

        foreach (var config in configurations)
        {
            var axis = new Axis(20, 12);
            config(axis);
            for (var i = 0; i <= axis.Count; i++)
            {
                Assert.Equal(NaivePosition(axis, i), axis.GetPositionOf(i));
            }
        }
    }

    [Fact]
    public void Matrix_GetIndexAt_MatchesNaive_AcrossConfigurations()
    {
        var configurations = new[]
        {
            new Action<Axis>(_ => { }),
            a => { a[3] = 50; a[7] = 5; },
            a => { a.Hide(0); a.Hide(4); a.Hide(9); },
            a => { a[2] = 30; a.Hide(2); },
            a => { a[1] = 100; a.Hide(5); a[8] = 1; },
        };

        foreach (var config in configurations)
        {
            var axis = new Axis(20, 12);
            config(axis);
            for (var pixel = 0d; pixel <= axis.Total + 50; pixel += 1.5)
            {
                Assert.Equal(NaiveIndexAt(axis, pixel), axis.GetIndexAt(pixel));
            }
        }
    }

    [Fact]
    public void SheetView_GetRowPixelRange_MatchesNaiveSum()
    {
        var sheet = new Worksheet(20, 5);
        sheet.Rows[3] = 50;
        sheet.Rows[10] = 5;
        sheet.Rows.Hide(7);

        var view = new SheetView(sheet);
        var range = view.GetRowPixelRange(5, 12);

        var expectedStart = NaivePosition(sheet.Rows, 5) + view.RowHeaderOffset;
        var expectedEnd = NaivePosition(sheet.Rows, 13) + view.RowHeaderOffset;

        Assert.Equal(expectedStart, range.Start);
        Assert.Equal(expectedEnd, range.End);
    }

    [Fact]
    public void SheetView_GetRowRange_FindsScrolledRow()
    {
        var sheet = new Worksheet(100, 5);
        var view = new SheetView(sheet);

        // Default row size from the worksheet
        var defaultRowSize = sheet.Rows.Size;

        // Scroll past 30 rows.
        var scrollTop = defaultRowSize * 30 + view.RowHeaderOffset;
        var viewportHeight = defaultRowSize * 10;

        var range = view.GetRowRange(scrollTop, scrollTop + viewportHeight);

        Assert.Equal(30, range.Start);
        Assert.True(range.End >= 39 && range.End <= 41);
    }

    [Fact]
    public void SheetView_GetRowPixelRange_AfterInsert_ReflectsNewLayout()
    {
        var sheet = new Worksheet(20, 5);
        sheet.Rows[5] = 100;
        var view = new SheetView(sheet);

        var before = view.GetRowPixelRange(10).Start;

        // Insert a row at index 3 — row 10 becomes row 11.
        sheet.Rows.ShiftDown(3, 1);
        sheet.Rows.Count = 21;

        var after = view.GetRowPixelRange(11).Start;
        Assert.Equal(before + sheet.Rows.Size, after);
    }
}
