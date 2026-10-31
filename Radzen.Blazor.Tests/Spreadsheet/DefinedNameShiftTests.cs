using Radzen.Documents.Spreadsheet;
using Xunit;

namespace Radzen.Blazor.Spreadsheet.Tests;

public class DefinedNameShiftTests
{
    private static (Workbook Workbook, Worksheet Sheet) CreateWorkbook()
    {
        var workbook = new Workbook();
        var sheet = workbook.AddSheet("Sheet1", 20, 8);

        for (var row = 0; row < 10; row++)
        {
            sheet.Cells[row, 0].Value = row + 1d;
        }

        return (workbook, sheet);
    }

    private static void DeleteRows(Worksheet sheet, int start, int end) =>
        Assert.True(new SheetView(sheet).Commands.Execute(new DeleteRowsCommand(sheet, start, end)));

    [Theory]
    [InlineData("Sheet1!$A$5", 1, 1, "Sheet1!$A$4")]
    [InlineData("Sheet1!$A$2", 1, 1, "Sheet1!#REF!")]
    [InlineData("Sheet1!$A$2:$A$5", 1, 1, "Sheet1!$A$2:$A$4")]
    [InlineData("Sheet1!$A$2:$A$5", 1, 4, "Sheet1!#REF!")]
    [InlineData("Sheet1!$2:$5", 1, 2, "Sheet1!$2:$3")]
    [InlineData("Sheet1!$A:$A", 1, 1, "Sheet1!$A:$A")]
    public void DeleteRows_UpdatesTheNameLikeExcel(string refersTo, int start, int end, string expected)
    {
        var (workbook, sheet) = CreateWorkbook();
        workbook.DefinedNames["N"] = refersTo;

        DeleteRows(sheet, start, end);

        Assert.Equal(expected, workbook.DefinedNames["N"]);
    }

    [Fact]
    public void InsertRow_UpdatesTheName()
    {
        var (workbook, sheet) = CreateWorkbook();
        workbook.DefinedNames["N"] = "Sheet1!$A$2:$A$5";

        sheet.InsertRow(2, 2);

        Assert.Equal("Sheet1!$A$2:$A$7", workbook.DefinedNames["N"]);
    }

    [Fact]
    public void DeleteColumn_UpdatesTheName()
    {
        var (workbook, sheet) = CreateWorkbook();
        workbook.DefinedNames["N"] = "Sheet1!$B$1:$E$1";

        Assert.True(new SheetView(sheet).Commands.Execute(new DeleteColumnsCommand(sheet, 2, 2)));

        Assert.Equal("Sheet1!$B$1:$D$1", workbook.DefinedNames["N"]);
    }

    [Fact]
    public void Delete_KeepsFormulaUsingTheNameOnTheSameValue()
    {
        var (workbook, sheet) = CreateWorkbook();
        workbook.DefinedNames["N"] = "Sheet1!$A$5";
        sheet.Cells["C1"].Formula = "=N*2";

        DeleteRows(sheet, 1, 1);

        Assert.Equal(10d, sheet.Cells["C1"].Value);
    }

    [Fact]
    public void Delete_KeepsDependencyOnTheMovedCell()
    {
        var (workbook, sheet) = CreateWorkbook();
        workbook.DefinedNames["N"] = "Sheet1!$A$5";
        sheet.Cells["C1"].Formula = "=N*2";

        DeleteRows(sheet, 1, 1);
        sheet.Cells["A4"].Value = 100d;

        Assert.Equal(200d, sheet.Cells["C1"].Value);
    }

    [Fact]
    public void Delete_TurnsFormulaUsingADeletedNameIntoRefError()
    {
        var (workbook, sheet) = CreateWorkbook();
        workbook.DefinedNames["N"] = "Sheet1!$A$2";
        sheet.Cells["C1"].Formula = "=N*2";

        DeleteRows(sheet, 1, 1);

        Assert.Equal(CellError.Ref, sheet.Cells["C1"].Value);
    }

    [Fact]
    public void Delete_FollowsNameThatRefersToAnotherName()
    {
        var (workbook, sheet) = CreateWorkbook();
        workbook.DefinedNames["Inner"] = "Sheet1!$A$5";
        workbook.DefinedNames["Outer"] = "Inner*2";
        sheet.Cells["C1"].Formula = "=Outer";

        DeleteRows(sheet, 1, 1);
        sheet.Cells["A4"].Value = 100d;

        Assert.Equal("Inner*2", workbook.DefinedNames["Outer"]);
        Assert.Equal(200d, sheet.Cells["C1"].Value);
    }

    [Fact]
    public void Delete_UpdatesFormulaUsingTheNameOnAnotherSheet()
    {
        var (workbook, sheet) = CreateWorkbook();
        var other = workbook.AddSheet("Other", 5, 5);
        workbook.DefinedNames["N"] = "Sheet1!$A$5";
        other.Cells["A1"].Formula = "=N";

        DeleteRows(sheet, 1, 1);
        sheet.Cells["A4"].Value = 100d;

        Assert.Equal(100d, other.Cells["A1"].Value);
    }

    [Fact]
    public void Delete_UpdatesSheetScopedName()
    {
        var (_, sheet) = CreateWorkbook();
        sheet.DefinedNames["N"] = "$A$5";

        DeleteRows(sheet, 1, 1);

        Assert.Equal("$A$4", sheet.DefinedNames["N"]);
    }

    [Fact]
    public void Delete_LeavesSheetScopedNameOfAnotherSheetAlone()
    {
        var (workbook, sheet) = CreateWorkbook();
        var other = workbook.AddSheet("Other", 5, 5);
        other.DefinedNames["N"] = "$A$5";

        DeleteRows(sheet, 1, 1);

        Assert.Equal("$A$5", other.DefinedNames["N"]);
    }

    [Theory]
    [InlineData("$A$5")]
    [InlineData("0.5")]
    [InlineData("Other!$A$5")]
    [InlineData("Sheet1!$A$1:$A$2,Sheet1!$C$1")]
    public void Delete_LeavesNameThatDoesNotPointAtTheSheet(string refersTo)
    {
        var (workbook, sheet) = CreateWorkbook();
        workbook.AddSheet("Other", 10, 5);
        workbook.DefinedNames["N"] = refersTo;

        DeleteRows(sheet, 1, 1);

        Assert.Equal(refersTo, workbook.DefinedNames["N"]);
    }

    [Fact]
    public void Undo_RestoresNameAndFormulaValue()
    {
        var (workbook, sheet) = CreateWorkbook();
        workbook.DefinedNames["Shrunk"] = "Sheet1!$A$2:$A$5";
        workbook.DefinedNames["Gone"] = "Sheet1!$A$2";
        sheet.Cells["C1"].Formula = "=SUM(Shrunk)+Gone";
        var view = new SheetView(sheet);

        Assert.True(view.Commands.Execute(new DeleteRowsCommand(sheet, 1, 1)));
        view.Commands.Undo();

        Assert.Equal("Sheet1!$A$2:$A$5", workbook.DefinedNames["Shrunk"]);
        Assert.Equal("Sheet1!$A$2", workbook.DefinedNames["Gone"]);
        Assert.Equal(16d, sheet.Cells["C1"].Value);
    }

    [Fact]
    public void Delete_TracksTheMovedRangeOfTheName()
    {
        var (workbook, sheet) = CreateWorkbook();
        workbook.DefinedNames["N"] = "Sheet1!$A$5:$A$6";
        sheet.Cells["C1"].Formula = "=SUM(N)";

        DeleteRows(sheet, 1, 1);
        sheet.Cells["A4"].Value = 100d;

        Assert.Equal(106d, sheet.Cells["C1"].Value);
    }

    [Fact]
    public void Insert_TracksTheMovedRangeOfTheName()
    {
        var (workbook, sheet) = CreateWorkbook();
        workbook.DefinedNames["N"] = "Sheet1!$A$5:$A$6";
        sheet.Cells["C1"].Formula = "=SUM(N)";

        sheet.InsertRow(1);
        sheet.Cells["A7"].Value = 100d;

        Assert.Equal(105d, sheet.Cells["C1"].Value);
    }

    [Fact]
    public void Delete_RefreshesFormulaOnAnotherSheetWhenTheNameIsDeleted()
    {
        var (workbook, sheet) = CreateWorkbook();
        var other = workbook.AddSheet("Other", 5, 5);
        workbook.DefinedNames["N"] = "Sheet1!$A$2";
        other.Cells["A1"].Formula = "=N";

        DeleteRows(sheet, 1, 1);

        Assert.Equal(CellError.Ref, other.Cells["A1"].Value);
    }

    [Fact]
    public void Undo_TracksTheOriginalRangeOfTheName()
    {
        var (workbook, sheet) = CreateWorkbook();
        workbook.DefinedNames["N"] = "Sheet1!$A$5:$A$6";
        sheet.Cells["C1"].Formula = "=SUM(N)";
        var view = new SheetView(sheet);

        Assert.True(view.Commands.Execute(new DeleteRowsCommand(sheet, 1, 1)));
        view.Commands.Undo();
        sheet.Cells["A6"].Value = 100d;

        Assert.Equal(105d, sheet.Cells["C1"].Value);
    }

    [Fact]
    public void Undo_RefreshesFormulaOnAnotherSheetWhenTheNameComesBack()
    {
        var (workbook, sheet) = CreateWorkbook();
        var other = workbook.AddSheet("Other", 5, 5);
        workbook.DefinedNames["N"] = "Sheet1!$A$2";
        other.Cells["A1"].Formula = "=N";
        var view = new SheetView(sheet);

        Assert.True(view.Commands.Execute(new DeleteRowsCommand(sheet, 1, 1)));
        view.Commands.Undo();

        Assert.Equal(2d, other.Cells["A1"].Value);
    }

    [Fact]
    public void InsertUndo_RestoresName()
    {
        var (workbook, sheet) = CreateWorkbook();
        workbook.DefinedNames["N"] = "Sheet1!$A$2:$A$5";
        var view = new SheetView(sheet);

        Assert.True(view.Commands.Execute(new InsertRowCommand(sheet, 2)));
        Assert.Equal("Sheet1!$A$2:$A$6", workbook.DefinedNames["N"]);

        view.Commands.Undo();

        Assert.Equal("Sheet1!$A$2:$A$5", workbook.DefinedNames["N"]);
    }

    [Fact]
    public void Undo_KeepsDependencyOnTheOriginalCell()
    {
        var (workbook, sheet) = CreateWorkbook();
        workbook.DefinedNames["N"] = "Sheet1!$A$5";
        sheet.Cells["C1"].Formula = "=N";
        var view = new SheetView(sheet);

        Assert.True(view.Commands.Execute(new DeleteRowsCommand(sheet, 1, 1)));
        view.Commands.Undo();
        sheet.Cells["A5"].Value = 100d;

        Assert.Equal(100d, sheet.Cells["C1"].Value);
    }
}
