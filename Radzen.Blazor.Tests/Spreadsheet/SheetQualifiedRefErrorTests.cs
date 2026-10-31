using System.IO;
using Radzen.Documents.Spreadsheet;
using Xunit;

namespace Radzen.Blazor.Spreadsheet.Tests;

public class SheetQualifiedRefErrorTests
{
    private static (Workbook Workbook, Worksheet First, Worksheet Second) CreateWorkbook()
    {
        var workbook = new Workbook();
        var first = workbook.AddSheet("First", 10, 10);
        var second = workbook.AddSheet("My Sheet", 10, 10);
        return (workbook, first, second);
    }

    [Theory]
    [InlineData("=First!#REF!")]
    [InlineData("='My Sheet'!#REF!")]
    [InlineData("=SUM(First!#REF!)")]
    [InlineData("=First!#REF!+A1")]
    public void Formula_ParsesAndEvaluatesToRefError(string formula)
    {
        var (_, first, _) = CreateWorkbook();
        first.Cells["B1"].Formula = formula;

        Assert.Empty(first.Cells["B1"].FormulaSyntaxTree!.Errors);
        Assert.Equal(CellError.Ref, first.Cells["B1"].Value);
    }

    [Fact]
    public void InsertRow_KeepsTheSheetQualifier()
    {
        var (_, first, _) = CreateWorkbook();
        first.Cells["B2"].Formula = "='My Sheet'!#REF!+A1";

        first.InsertRow(0);

        Assert.Equal("='My Sheet'!#REF!+A2", first.Cells["B3"].Formula);
    }

    [Fact]
    public void RenamingTheSheet_RenamesTheQualifier()
    {
        var (_, first, second) = CreateWorkbook();
        first.Cells["B1"].Formula = "=SUM('My Sheet'!#REF!)";

        second.Name = "Renamed";

        Assert.Equal("=SUM(Renamed!#REF!)", first.Cells["B1"].Formula);
    }

    [Fact]
    public void XlsxRoundTrip_KeepsTheFormulaAndTheError()
    {
        var (workbook, first, _) = CreateWorkbook();
        first.Cells["B1"].Formula = "='My Sheet'!#REF!";
        using var stream = new MemoryStream();

        workbook.SaveToStream(stream);
        stream.Position = 0;
        var loaded = Workbook.LoadFromStream(stream);

        Assert.Equal("='My Sheet'!#REF!", loaded.Sheets[0].Cells["B1"].Formula);
        Assert.Equal(CellError.Ref, loaded.Sheets[0].Cells["B1"].Value);
    }
}
