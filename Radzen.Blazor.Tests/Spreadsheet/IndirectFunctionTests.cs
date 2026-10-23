using System.IO;
using Xunit;

using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet.Tests;

public class IndirectFunctionTests
{
    readonly Workbook workbook = new();
    readonly Worksheet sheet;
    readonly Worksheet material;

    public IndirectFunctionTests()
    {
        sheet = workbook.AddSheet("Main", 50, 20);
        material = workbook.AddSheet("Material, Subs & Fab", 50, 20);
        sheet.Cells["A1"].Value = 1;
        sheet.Cells["A2"].Value = 2;
        sheet.Cells["A3"].Value = 3;
        sheet.Cells["B1"].Value = "A2";
        material.Cells["L1"].Value = 100;
        material.Cells["L2"].Value = 200;
        workbook.DefinedNames["Nums"] = "Main!$A$1:$A$3";
        workbook.DefinedNames["Seven"] = "7";
    }

    private object? Evaluate(string address, string formula)
    {
        sheet.Cells[address].Formula = formula;
        return sheet.Cells[address].Value;
    }

    [Theory]
    [InlineData("M20", "=INDIRECT(\"A2\")", 2d)]
    [InlineData("M21", "=INDIRECT(B1)", 2d)]
    [InlineData("M22", "=SUM(INDIRECT(\"A1:A3\"))", 6d)]
    [InlineData("M23", "=SUM(INDIRECT(\"A1:A\"&ROW()-20))", 6d)]
    [InlineData("M24", "=INDIRECT(\"'Material, Subs & Fab'!L2\")", 200d)]
    [InlineData("M25", "=SUM(INDIRECT(\"Nums\"))", 6d)]
    [InlineData("M35", "=ROW(INDIRECT(\"A5\"))", 5d)]
    [InlineData("M36", "=ROWS(INDIRECT(\"A1:A3\"))", 3d)]
    [InlineData("M37", "=INDIRECT(\"a2\")", 2d)]
    [InlineData("M39", "=INDIRECT(\"$A$2\")", 2d)]
    [InlineData("M40", "=INDIRECT(\"A:A\")", 0d)]
    public void A1TextResolvesToTheReference(string address, string formula, double expected)
    {
        Assert.Equal(expected, Evaluate(address, formula));
    }

    [Theory]
    [InlineData("M30", "=INDIRECT(\"R2C1\",FALSE)", 2d)]
    [InlineData("M31", "=INDIRECT(\"R[-29]C[-12]\",FALSE)", 2d)]
    [InlineData("M32", "=SUM(INDIRECT(\"R1C1:R3C1\",FALSE))", 6d)]
    [InlineData("A3", "=INDIRECT(\"R[-1]C\",FALSE)", 2d)]
    public void R1C1TextResolvesRelativeToTheFormulaCell(string address, string formula, double expected)
    {
        Assert.Equal(expected, Evaluate(address, formula));
    }

    [Theory]
    [InlineData("=INDIRECT(\"Seven\")")]
    [InlineData("=INDIRECT(\"NoSuchSheet!A1\")")]
    [InlineData("=INDIRECT(\"\")")]
    [InlineData("=INDIRECT(\"1+1\")")]
    [InlineData("=INDIRECT(\" A2 \")")]
    [InlineData("=INDIRECT(\"A2\",0)")]
    [InlineData("=INDIRECT(\"[1]Sheet!A1\")")]
    public void TextThatIsNotAReferenceReturnsRefError(string formula)
    {
        Assert.Equal(CellError.Ref, Evaluate("M20", formula));
    }

    [Fact]
    public void NonBooleanA1ArgumentReturnsValueError()
    {
        Assert.Equal(CellError.Value, Evaluate("M20", "=INDIRECT(\"A2\",\"x\")"));
    }

    [Fact]
    public void ChangingTheTargetRecalculatesTheIndirectFormulaAndItsDependents()
    {
        sheet.Cells["M20"].Formula = "=INDIRECT(\"A2\")";
        sheet.Cells["M22"].Formula = "=SUM(INDIRECT(\"A1:A3\"))";
        sheet.Cells["N22"].Formula = "=M22*10";

        sheet.Cells["A2"].Value = 50;

        Assert.Equal(50d, sheet.Cells["M20"].Value);
        Assert.Equal(54d, sheet.Cells["M22"].Value);
        Assert.Equal(540d, sheet.Cells["N22"].Value);
    }

    [Fact]
    public void ChangingTheTargetOnAnotherSheetRecalculatesTheIndirectFormula()
    {
        sheet.Cells["M20"].Formula = "=INDIRECT(\"'Material, Subs & Fab'!L2\")";

        material.Cells["L2"].Value = 7;

        Assert.Equal(7d, sheet.Cells["M20"].Value);
    }

    [Fact]
    public void TargetFormulasAreCalculatedBeforeTheIndirectFormulaWhenLoadingXlsx()
    {
        material.Cells["L14"].Value = 10;
        material.Cells["L15"].Formula = "=L14*2";
        material.Cells["L16"].Formula = "=SUM(INDIRECT(\"L14:L\"&ROW()-1))";
        material.Cells["L17"].Formula = "=SUM(L16:L16)";
        sheet.Cells["H35"].Formula = "='Material, Subs & Fab'!L17";

        using var stream = new MemoryStream();
        workbook.SaveToStream(stream);
        stream.Position = 0;
        var loaded = Workbook.LoadFromStream(stream);

        Assert.Equal(30d, loaded.Sheets[0].Cells["H35"].Value);

        loaded.Sheets[1].Cells["L14"].Value = 20;

        Assert.Equal(60d, loaded.Sheets[0].Cells["H35"].Value);
    }
}
