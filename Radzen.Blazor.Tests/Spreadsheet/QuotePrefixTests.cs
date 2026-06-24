using System.IO;
using Xunit;

using Radzen.Documents.Spreadsheet;
using Radzen.Blazor.Spreadsheet;

namespace Radzen.Blazor.Spreadsheet.Tests;

public class QuotePrefixTests
{
    readonly Worksheet sheet = new(5, 5);

    [Fact]
    public void SetValue_WithLeadingApostropheOnFormula_StoresRemainderAsText()
    {
        sheet.Cells["A1"].SetValue("'=SUM(B1:B2)");

        Assert.Equal("=SUM(B1:B2)", sheet.Cells["A1"].Value);
        Assert.Null(sheet.Cells["A1"].Formula);
        Assert.Equal(CellDataType.String, sheet.Cells["A1"].ValueType);
    }

    [Fact]
    public void SetValue_WithLeadingApostropheOnFormula_SetsQuotePrefix()
    {
        sheet.Cells["A1"].SetValue("'=SUM(B1:B2)");

        Assert.True(sheet.Cells["A1"].QuotePrefix);
    }

    [Fact]
    public void SetValue_WithLeadingApostropheOnPlainText_StripsApostrophe()
    {
        sheet.Cells["A1"].SetValue("'hello");

        Assert.Equal("hello", sheet.Cells["A1"].Value);
        Assert.True(sheet.Cells["A1"].QuotePrefix);
    }

    [Fact]
    public void SetValue_ApostropheOnly_StoresEmptyStringWithQuotePrefix()
    {
        sheet.Cells["A1"].SetValue("'");

        Assert.Equal(string.Empty, sheet.Cells["A1"].Value);
        Assert.True(sheet.Cells["A1"].QuotePrefix);
    }

    [Fact]
    public void SetValue_WithoutApostrophe_DoesNotSetQuotePrefix()
    {
        sheet.Cells["A1"].SetValue("=SUM(B1:B2)");

        Assert.False(sheet.Cells["A1"].QuotePrefix);
        Assert.Equal("=SUM(B1:B2)", sheet.Cells["A1"].Formula);
    }

    [Fact]
    public void SetValue_ReassigningPlainText_ClearsPreviousQuotePrefix()
    {
        sheet.Cells["A1"].SetValue("'=SUM(B1:B2)");
        sheet.Cells["A1"].SetValue("plain");

        Assert.False(sheet.Cells["A1"].QuotePrefix);
        Assert.Equal("plain", sheet.Cells["A1"].Value);
    }

    [Fact]
    public void GetValue_OnQuotePrefixedCell_ReturnsValueWithLeadingApostrophe()
    {
        sheet.Cells["A1"].SetValue("'=SUM(B1:B2)");

        Assert.Equal("'=SUM(B1:B2)", sheet.Cells["A1"].GetValue());
    }

    [Fact]
    public void GetValue_OnPlainTextCell_DoesNotAddApostrophe()
    {
        sheet.Cells["A1"].SetValue("hello");

        Assert.Equal("hello", sheet.Cells["A1"].GetValue());
    }

    [Fact]
    public void Formula_ReferencingQuotePrefixedCell_ReturnsLiteralString()
    {
        sheet.Cells["A1"].SetValue("'=SUM(B1:B2)");
        sheet.Cells["B1"].Value = 10d;
        sheet.Cells["B2"].Value = 20d;
        sheet.Cells["A2"].Formula = "=A1";

        Assert.Equal("=SUM(B1:B2)", sheet.Cells["A2"].Value);
    }

    [Fact]
    public void Editor_AcceptingApostropheEscapedInput_CommitsAsText()
    {
        var editor = new Editor(sheet);

        editor.StartEdit(CellRef.Parse("A1"), "'=SUM(B1:B2)");
        editor.Accept();

        Assert.Equal("=SUM(B1:B2)", sheet.Cells["A1"].Value);
        Assert.Null(sheet.Cells["A1"].Formula);
        Assert.True(sheet.Cells["A1"].QuotePrefix);
    }

    [Fact]
    public void Editor_ReEnteringQuotePrefixedCell_ShowsApostropheInEditor()
    {
        sheet.Cells["A1"].SetValue("'=SUM(B1:B2)");
        var editor = new Editor(sheet);

        editor.StartEdit(CellRef.Parse("A1"), sheet.Cells["A1"].GetValue());

        Assert.Equal("'=SUM(B1:B2)", editor.Value);
    }

    [Fact]
    public void Value_DirectAssignment_ClearsQuotePrefix()
    {
        sheet.Cells["A1"].SetValue("'=SUM(B1:B2)");
        sheet.Cells["A1"].Value = "plain";

        Assert.False(sheet.Cells["A1"].QuotePrefix);
    }

    [Fact]
    public void Formula_DirectAssignment_ClearsQuotePrefix()
    {
        sheet.Cells["A1"].SetValue("'=SUM(B1:B2)");
        sheet.Cells["A1"].Formula = "=B1";

        Assert.False(sheet.Cells["A1"].QuotePrefix);
    }

    [Fact]
    public void Clone_PreservesQuotePrefix()
    {
        sheet.Cells["A1"].SetValue("'=SUM(B1:B2)");
        var clone = sheet.Cells["A1"].Clone();

        Assert.True(clone.QuotePrefix);
        Assert.Equal("=SUM(B1:B2)", clone.Value);
    }

    [Fact]
    public void CopyFrom_PreservesQuotePrefix()
    {
        sheet.Cells["A1"].SetValue("'=SUM(B1:B2)");
        sheet.Cells["A2"].CopyFrom(sheet.Cells["A1"]);

        Assert.True(sheet.Cells["A2"].QuotePrefix);
        Assert.Equal("=SUM(B1:B2)", sheet.Cells["A2"].Value);
    }

    [Fact]
    public void CopyFrom_OverwritesPriorQuotePrefix()
    {
        sheet.Cells["A1"].SetValue("plain");
        sheet.Cells["A2"].SetValue("'=SUM(B1:B2)");
        sheet.Cells["A2"].CopyFrom(sheet.Cells["A1"]);

        Assert.False(sheet.Cells["A2"].QuotePrefix);
    }

    [Fact]
    public void RoundTrip_QuotePrefix_Preserved()
    {
        var workbook = new Workbook();
        var original = new Worksheet(5, 5);
        workbook.AddSheet(original);
        original.Cells["A1"].SetValue("'=SUM(B1:B2)");

        using var ms = new MemoryStream();
        workbook.SaveToStream(ms);
        ms.Position = 0;
        var reimported = Workbook.LoadFromStream(ms);

        var reloaded = reimported.Sheets[0].Cells["A1"];
        Assert.True(reloaded.QuotePrefix);
        Assert.Equal("=SUM(B1:B2)", reloaded.Value);
        Assert.Null(reloaded.Formula);
    }

    [Fact]
    public void RoundTrip_QuotePrefixedValue_NotDoubled()
    {
        var workbook = new Workbook();
        var original = new Worksheet(5, 5);
        workbook.AddSheet(original);
        original.Cells["A1"].SetValue("'=SUM(B1:B2)");

        using var ms = new MemoryStream();
        workbook.SaveToStream(ms);
        ms.Position = 0;
        var reimported = Workbook.LoadFromStream(ms);

        Assert.Equal("'=SUM(B1:B2)", reimported.Sheets[0].Cells["A1"].GetValue());
    }
}
