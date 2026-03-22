using Xunit;

using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet.Tests;

public class MergeCellsCommandTests
{
    [Fact]
    public void Execute_MergesCells()
    {
        var sheet = new Worksheet(10, 10);
        sheet.Cells[0, 0].Value = "A";
        sheet.Cells[0, 1].Value = "B";
        var range = new RangeRef(new CellRef(0, 0), new CellRef(0, 1));

        var command = new MergeCellsCommand(sheet, range);
        Assert.True(command.Execute());
        Assert.True(sheet.MergedCells.Contains(new CellRef(0, 0)));
        Assert.Null(sheet.Cells[0, 1].Value);
    }

    [Fact]
    public void Unexecute_RestoresCells()
    {
        var sheet = new Worksheet(10, 10);
        sheet.Cells[0, 0].Value = "A";
        sheet.Cells[0, 1].Value = "B";
        var range = new RangeRef(new CellRef(0, 0), new CellRef(0, 1));

        var command = new MergeCellsCommand(sheet, range);
        command.Execute();
        command.Unexecute();

        Assert.False(sheet.MergedCells.Contains(new CellRef(0, 0)));
        Assert.Equal("B", sheet.Cells[0, 1].Value);
    }

    [Fact]
    public void Execute_RejectsOverlap()
    {
        var sheet = new Worksheet(10, 10);
        var range1 = new RangeRef(new CellRef(0, 0), new CellRef(0, 1));
        sheet.MergedCells.Add(range1);

        var range2 = new RangeRef(new CellRef(0, 0), new CellRef(1, 1));
        var command = new MergeCellsCommand(sheet, range2);
        Assert.False(command.Execute());
    }

    [Fact]
    public void Execute_MergeAndCenter_SetsAlignment()
    {
        var sheet = new Worksheet(10, 10);
        sheet.Cells[0, 0].Value = "A";
        var range = new RangeRef(new CellRef(0, 0), new CellRef(0, 1));

        var command = new MergeCellsCommand(sheet, range, center: true);
        command.Execute();

        Assert.Equal(TextAlign.Center, sheet.Cells[0, 0].Format.TextAlign);
    }
}

public class UnmergeCellsCommandTests
{
    [Fact]
    public void Execute_UnmergesCells()
    {
        var sheet = new Worksheet(10, 10);
        var range = new RangeRef(new CellRef(0, 0), new CellRef(0, 1));
        sheet.MergedCells.Add(range);

        var command = new UnmergeCellsCommand(sheet, new CellRef(0, 0));
        Assert.True(command.Execute());
        Assert.False(sheet.MergedCells.Contains(new CellRef(0, 0)));
    }

    [Fact]
    public void Unexecute_RestoresMerge()
    {
        var sheet = new Worksheet(10, 10);
        var range = new RangeRef(new CellRef(0, 0), new CellRef(0, 1));
        sheet.MergedCells.Add(range);

        var command = new UnmergeCellsCommand(sheet, new CellRef(0, 0));
        command.Execute();
        command.Unexecute();

        Assert.True(sheet.MergedCells.Contains(new CellRef(0, 0)));
    }
}

public class BorderCommandTests
{
    [Fact]
    public void AllBordersCommand_AppliesBordersToAllCells()
    {
        var sheet = new Worksheet(10, 10);
        sheet.Cells[0, 0].Value = "A";
        var range = new RangeRef(new CellRef(0, 0), new CellRef(1, 1));
        var border = new BorderStyle { LineStyle = BorderLineStyle.Thin };

        var command = new AllBordersCommand(sheet, range, border);
        Assert.True(command.Execute());

        Assert.NotNull(sheet.Cells[0, 0].Format.BorderTop);
        Assert.NotNull(sheet.Cells[0, 0].Format.BorderRight);
        Assert.NotNull(sheet.Cells[0, 0].Format.BorderBottom);
        Assert.NotNull(sheet.Cells[0, 0].Format.BorderLeft);
        Assert.NotNull(sheet.Cells[1, 1].Format.BorderTop);
    }

    [Fact]
    public void AllBordersCommand_Unexecute_RestoresOriginal()
    {
        var sheet = new Worksheet(10, 10);
        sheet.Cells[0, 0].Value = "A";
        var range = new RangeRef(new CellRef(0, 0), new CellRef(0, 0));
        var border = new BorderStyle();

        var command = new AllBordersCommand(sheet, range, border);
        command.Execute();
        command.Unexecute();

        Assert.Null(sheet.Cells[0, 0].Format.BorderTop);
    }

    [Fact]
    public void NoBordersCommand_ClearsBorders()
    {
        var sheet = new Worksheet(10, 10);
        sheet.Cells[0, 0].Format.BorderTop = new BorderStyle();
        var range = new RangeRef(new CellRef(0, 0), new CellRef(0, 0));

        var command = new NoBordersCommand(sheet, range);
        command.Execute();

        Assert.Null(sheet.Cells[0, 0].Format.BorderTop);
    }
}
