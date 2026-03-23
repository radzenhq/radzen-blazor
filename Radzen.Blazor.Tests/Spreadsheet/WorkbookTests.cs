using System;
using Xunit;

using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet.Tests;

#nullable enable

public class WorkbookTests
{
    [Fact]
    public void AddSheet_ShouldAddSheet()
    {
        var workbook = new Workbook();
        var sheet = workbook.AddSheet("Sheet1", 10, 10);

        Assert.Single(workbook.Sheets);
        Assert.Equal("Sheet1", sheet.Name);
    }

    [Fact]
    public void RemoveSheet_ShouldRemoveSheet()
    {
        var workbook = new Workbook();
        var sheet1 = workbook.AddSheet("Sheet1", 10, 10);
        workbook.AddSheet("Sheet2", 10, 10);

        var result = workbook.RemoveSheet(sheet1);

        Assert.True(result);
        Assert.Single(workbook.Sheets);
        Assert.Equal("Sheet2", workbook.Sheets[0].Name);
    }

    [Fact]
    public void RemoveSheet_ShouldReturnFalseForUnknownSheet()
    {
        var workbook = new Workbook();
        workbook.AddSheet("Sheet1", 10, 10);

        var other = new Worksheet(10, 10);
        var result = workbook.RemoveSheet(other);

        Assert.False(result);
        Assert.Single(workbook.Sheets);
    }

    [Fact]
    public void IndexOf_ShouldReturnCorrectIndex()
    {
        var workbook = new Workbook();
        workbook.AddSheet("Sheet1", 10, 10);
        var sheet2 = workbook.AddSheet("Sheet2", 10, 10);
        workbook.AddSheet("Sheet3", 10, 10);

        Assert.Equal(1, workbook.IndexOf(sheet2));
    }

    [Fact]
    public void IndexOf_ShouldReturnNegativeOneForUnknownSheet()
    {
        var workbook = new Workbook();
        workbook.AddSheet("Sheet1", 10, 10);

        var other = new Worksheet(10, 10);

        Assert.Equal(-1, workbook.IndexOf(other));
    }

    [Fact]
    public void MoveSheet_ShouldSwapPositions()
    {
        var workbook = new Workbook();
        workbook.AddSheet("Sheet1", 10, 10);
        workbook.AddSheet("Sheet2", 10, 10);
        workbook.AddSheet("Sheet3", 10, 10);

        workbook.MoveSheet(0, 2);

        Assert.Equal("Sheet2", workbook.Sheets[0].Name);
        Assert.Equal("Sheet3", workbook.Sheets[1].Name);
        Assert.Equal("Sheet1", workbook.Sheets[2].Name);
    }

    [Fact]
    public void MoveSheet_ShouldThrowForInvalidIndex()
    {
        var workbook = new Workbook();
        workbook.AddSheet("Sheet1", 10, 10);

        Assert.Throws<ArgumentOutOfRangeException>(() => workbook.MoveSheet(-1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => workbook.MoveSheet(0, 1));
    }
}
