using Xunit;

using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet.Tests;

public class ConditionalFormatRuleTests
{
    private static Cell CreateCell(object? value)
    {
        var sheet = new Worksheet(10, 10);
        sheet.Cells[0, 0].Value = value;
        return sheet.Cells[0, 0];
    }

    [Fact]
    public void GreaterThanRule_Matches()
    {
        var rule = new GreaterThanRule { Value = 10, Format = new Format { Bold = true } };
        Assert.NotNull(rule.GetFormat(CreateCell(15)));
        Assert.Null(rule.GetFormat(CreateCell(5)));
        Assert.Null(rule.GetFormat(CreateCell(10)));
    }

    [Fact]
    public void LessThanRule_Matches()
    {
        var rule = new LessThanRule { Value = 10, Format = new Format { Italic = true } };
        Assert.NotNull(rule.GetFormat(CreateCell(5)));
        Assert.Null(rule.GetFormat(CreateCell(15)));
    }

    [Fact]
    public void BetweenRule_Matches()
    {
        var rule = new BetweenRule { Minimum = 5, Maximum = 15, Format = new Format { Bold = true } };
        Assert.NotNull(rule.GetFormat(CreateCell(10)));
        Assert.NotNull(rule.GetFormat(CreateCell(5)));
        Assert.NotNull(rule.GetFormat(CreateCell(15)));
        Assert.Null(rule.GetFormat(CreateCell(20)));
        Assert.Null(rule.GetFormat(CreateCell(3)));
    }

    [Fact]
    public void TextContainsRule_Matches()
    {
        var rule = new TextContainsRule { Text = "hello", Format = new Format { Color = "red" } };
        Assert.NotNull(rule.GetFormat(CreateCell("Hello World")));
        Assert.Null(rule.GetFormat(CreateCell("Goodbye")));
    }

    [Fact]
    public void ConditionalFormatStore_Remove_Works()
    {
        var store = new ConditionalFormatStore();
        var range = new RangeRef(new CellRef(0, 0), new CellRef(5, 5));
        var rule = new GreaterThanRule { Value = 10, Format = new Format { Bold = true } };

        store.Add(range, rule);
        Assert.True(store.Remove(range, rule));
        Assert.False(store.Remove(range, rule));
    }

    [Fact]
    public void ConditionalFormatCommand_ExecuteAndUnexecute()
    {
        var sheet = new Worksheet(10, 10);
        sheet.Cells[0, 0].Value = 20;
        var range = new RangeRef(new CellRef(0, 0), new CellRef(0, 0));
        var rule = new GreaterThanRule { Value = 10, Format = new Format { Bold = true } };

        var command = new ConditionalFormatCommand(sheet, range, rule);
        Assert.True(command.Execute());

        // Verify rule was added
        var result = sheet.ConditionalFormats.Calculate(sheet.Cells[0, 0]);
        Assert.NotNull(result);
        Assert.True(result.Bold);

        command.Unexecute();

        // Verify rule was removed
        result = sheet.ConditionalFormats.Calculate(sheet.Cells[0, 0]);
        Assert.Null(result);
    }
}
