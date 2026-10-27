using System;
using System.Collections.Generic;
using Radzen.Documents.Spreadsheet;
using Xunit;

namespace Radzen.Blazor.Spreadsheet.Tests;

#nullable enable

public class CellStoreSetValuesTests
{
    private static IReadOnlyList<IReadOnlyList<object?>?> Block(params object?[]?[] rows) => rows;

    [Fact]
    public void FillsTheBlockFromTheGivenOrigin()
    {
        var sheet = new Workbook().AddSheet("Sheet1", 10, 10);

        sheet.Cells.SetValues(2, 1, Block(
            ["a", 1],
            ["b", 2]));

        Assert.Equal("a", sheet.Cells[2, 1].Value);
        Assert.Equal(1d, sheet.Cells[2, 2].Value);
        Assert.Equal("b", sheet.Cells[3, 1].Value);
        Assert.Equal(2d, sheet.Cells[3, 2].Value);

        Assert.True(sheet.Cells[2, 0].IsEmpty);
        Assert.True(sheet.Cells[4, 1].IsEmpty);
    }

    [Fact]
    public void AgreesWithTheIndexerCellForCell()
    {
        object?[][] rows =
        [
            ["text", 1, 2.5, true, new DateTime(2020, 3, 4)],
            [null, -1, 0.0, false, "trailing"],
        ];

        var bulk = new Workbook().AddSheet("Sheet1", 4, 5);
        bulk.Cells.SetValues(0, 0, rows);

        var loop = new Workbook().AddSheet("Sheet1", 4, 5);
        for (var r = 0; r < rows.Length; r++)
        {
            for (var c = 0; c < rows[r].Length; c++)
            {
                loop.Cells[r, c].Value = rows[r][c];
            }
        }

        for (var r = 0; r < rows.Length; r++)
        {
            for (var c = 0; c < rows[r].Length; c++)
            {
                Assert.Equal(loop.Cells[r, c].Value, bulk.Cells[r, c].Value);
                Assert.Equal(loop.Cells[r, c].ValueType, bulk.Cells[r, c].ValueType);
            }
        }
    }

    [Fact]
    public void OverwritesCellsThatAlreadyHaveValues()
    {
        var sheet = new Workbook().AddSheet("Sheet1", 4, 4);

        sheet.Cells[0, 0].Value = "old";
        sheet.Cells[0, 0].Format.Bold = true;

        sheet.Cells.SetValues(0, 0, Block(["new"]));

        Assert.Equal("new", sheet.Cells[0, 0].Value);

        Assert.True(sheet.Cells[0, 0].Format.Bold);
    }

    [Fact]
    public void RaggedAndNullRowsLeaveTheRestAlone()
    {
        var sheet = new Workbook().AddSheet("Sheet1", 5, 5);

        sheet.Cells.SetValues(0, 0, Block(
            ["a", "b", "c"],
            null,
            ["d"]));

        Assert.Equal("c", sheet.Cells[0, 2].Value);
        Assert.True(sheet.Cells[1, 0].IsEmpty);
        Assert.Equal("d", sheet.Cells[2, 0].Value);
        Assert.True(sheet.Cells[2, 1].IsEmpty);
    }

    [Fact]
    public void EmptyInputDoesNothing()
    {
        var sheet = new Workbook().AddSheet("Sheet1", 4, 4);

        sheet.Cells.SetValues(0, 0, Array.Empty<IReadOnlyList<object?>?>());
        sheet.Cells.SetValues(0, 0, Block(null, null));

        Assert.True(sheet.Cells[0, 0].IsEmpty);
    }

    [Fact]
    public void NullInputThrows() =>
        Assert.Throws<ArgumentNullException>(() =>
            new Workbook().AddSheet("Sheet1", 4, 4).Cells.SetValues(0, 0, null!));

    [Theory]
    [InlineData(0, 0, 5, 2)]
    [InlineData(0, 0, 2, 5)]
    [InlineData(3, 3, 2, 2)]
    [InlineData(-1, 0, 1, 1)]
    public void ABlockThatDoesNotFitThrows(int row, int column, int height, int width)
    {
        var sheet = new Workbook().AddSheet("Sheet1", 4, 4);

        var rows = new IReadOnlyList<object?>?[height];
        for (var r = 0; r < height; r++)
        {
            rows[r] = new object?[width];
        }

        Assert.Throws<ArgumentOutOfRangeException>(() => sheet.Cells.SetValues(row, column, rows));
    }

    [Fact]
    public void FormulasOverTheBlockAreEvaluatedAfterIt()
    {
        var sheet = new Workbook().AddSheet("Sheet1", 5, 3);

        sheet.Cells[4, 0].Formula = "=SUM(A1:A3)";

        sheet.Cells.SetValues(0, 0, Block([1], [2], [3]));

        Assert.Equal(6d, sheet.Cells[4, 0].Value);
    }

    [Fact]
    public void AFormulaInsideTheBlockIsReplacedByTheValueWrittenOverIt()
    {
        var sheet = new Workbook().AddSheet("Sheet1", 5, 3);

        sheet.Cells[2, 0].Value = 10;
        sheet.Cells[0, 0].Formula = "=A3+1";

        Assert.Equal(11d, sheet.Cells[0, 0].Value);

        sheet.Cells.SetValues(0, 0, Block(["x"]));

        Assert.Equal("x", sheet.Cells[0, 0].Value);
        Assert.Null(sheet.Cells[0, 0].Formula);
    }

    [Fact]
    public void AFormulaOverAReplacedFormulaIsEvaluatedAgainstTheValue()
    {
        var sheet = new Workbook().AddSheet("Sheet1", 5, 3);

        sheet.Cells[2, 0].Value = 10;
        sheet.Cells[0, 0].Formula = "=A3+1";
        sheet.Cells[4, 0].Formula = "=A1*2";

        Assert.Equal(22d, sheet.Cells[4, 0].Value);

        sheet.Cells.SetValues(0, 0, Block([5]));

        Assert.Equal(5d, sheet.Cells[0, 0].Value);
        Assert.Equal(10d, sheet.Cells[4, 0].Value);
    }
}
