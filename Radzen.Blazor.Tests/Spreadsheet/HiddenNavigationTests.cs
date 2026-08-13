using Xunit;

using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet.Tests;

#nullable enable
public class HiddenNavigationTests
{
    private static Worksheet CreateSheet()
    {
        return new Worksheet(10, 10);
    }

    [Fact]
    public void Move_Right_SkipsHiddenColumns()
    {
        var sheet = CreateSheet();
        sheet.Columns.Hide(2);
        sheet.Columns.Hide(3);
        sheet.Columns.Hide(4);
        sheet.Selection.Select(new CellRef(3, 1));

        var address = sheet.Selection.Move(0, 1);

        Assert.Equal(new CellRef(3, 5), address);
    }

    [Fact]
    public void Move_Left_SkipsHiddenColumns()
    {
        var sheet = CreateSheet();
        sheet.Columns.Hide(2);
        sheet.Columns.Hide(3);
        sheet.Selection.Select(new CellRef(3, 4));

        var address = sheet.Selection.Move(0, -1);

        Assert.Equal(new CellRef(3, 1), address);
    }

    [Fact]
    public void Move_Down_SkipsHiddenRows()
    {
        var sheet = CreateSheet();
        sheet.Rows.Hide(4);
        sheet.Rows.Hide(5);
        sheet.Selection.Select(new CellRef(3, 0));

        var address = sheet.Selection.Move(1, 0);

        Assert.Equal(new CellRef(6, 0), address);
    }

    [Fact]
    public void Move_Up_SkipsHiddenRows()
    {
        var sheet = CreateSheet();
        sheet.Rows.Hide(2);
        sheet.Selection.Select(new CellRef(3, 0));

        var address = sheet.Selection.Move(-1, 0);

        Assert.Equal(new CellRef(1, 0), address);
    }

    [Fact]
    public void Move_Right_StaysPutWhenRemainingColumnsAreHidden()
    {
        var sheet = CreateSheet();
        sheet.Columns.Hide(8);
        sheet.Columns.Hide(9);
        sheet.Selection.Select(new CellRef(0, 7));

        var address = sheet.Selection.Move(0, 1);

        Assert.Equal(new CellRef(0, 7), address);
    }

    [Fact]
    public void Move_Left_StaysPutWhenLeadingColumnsAreHidden()
    {
        var sheet = CreateSheet();
        sheet.Columns.Hide(0);
        sheet.Columns.Hide(1);
        sheet.Selection.Select(new CellRef(0, 2));

        var address = sheet.Selection.Move(0, -1);

        Assert.Equal(new CellRef(0, 2), address);
    }

    [Fact]
    public void Cycle_Tab_SkipsHiddenColumnsWithinRange()
    {
        var sheet = CreateSheet();
        sheet.Columns.Hide(2);
        sheet.Columns.Hide(3);
        sheet.Selection.Select(new RangeRef(new CellRef(0, 0), new CellRef(1, 4)));

        var address = sheet.Selection.Cycle(0, 1);

        Assert.Equal(new CellRef(0, 1), address);

        address = sheet.Selection.Cycle(0, 1);

        Assert.Equal(new CellRef(0, 4), address);
    }

    [Fact]
    public void Cycle_Enter_SkipsHiddenRowsWithinRange()
    {
        var sheet = CreateSheet();
        sheet.Rows.Hide(1);
        sheet.Selection.Select(new RangeRef(new CellRef(0, 0), new CellRef(2, 1)));

        var address = sheet.Selection.Cycle(1, 0);

        Assert.Equal(new CellRef(2, 0), address);
    }

    [Fact]
    public void Cycle_Tab_WrapsToNextVisibleRow()
    {
        var sheet = CreateSheet();
        sheet.Rows.Hide(1);
        sheet.Selection.Select(new CellRef(0, 1), new RangeRef(new CellRef(0, 0), new CellRef(2, 1)));

        var address = sheet.Selection.Cycle(0, 1);

        Assert.Equal(new CellRef(2, 0), address);
    }

    [Fact]
    public void Extend_Right_SkipsHiddenColumns()
    {
        var sheet = CreateSheet();
        sheet.Columns.Hide(2);
        sheet.Selection.Select(new CellRef(0, 1));

        sheet.Selection.Extend(0, 1);

        Assert.Equal(new RangeRef(new CellRef(0, 1), new CellRef(0, 3)), sheet.Selection.Range);
    }

    [Fact]
    public void Extend_Down_KeepsRangeWhenRemainingRowsAreHidden()
    {
        var sheet = CreateSheet();
        sheet.Rows.Hide(8);
        sheet.Rows.Hide(9);
        sheet.Selection.Select(new CellRef(7, 0));

        sheet.Selection.Extend(1, 0);

        Assert.Equal(new RangeRef(new CellRef(7, 0), new CellRef(7, 0)), sheet.Selection.Range);
    }

    [Fact]
    public void FirstVisibleCell_SkipsLeadingHiddenRowsAndColumns()
    {
        var sheet = CreateSheet();
        sheet.Rows.Hide(0);
        sheet.Rows.Hide(1);
        sheet.Columns.Hide(0);

        Assert.Equal(new CellRef(2, 1), sheet.FirstVisibleCell());
    }

    [Fact]
    public void FirstVisibleCell_ReturnsOriginWhenNothingIsHidden()
    {
        var sheet = CreateSheet();

        Assert.Equal(new CellRef(0, 0), sheet.FirstVisibleCell());
    }
}
