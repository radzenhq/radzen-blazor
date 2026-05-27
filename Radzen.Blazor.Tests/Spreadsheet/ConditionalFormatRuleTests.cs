using Xunit;

using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet.Tests;
#nullable enable

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
    public void Top10Rule_Top3_MatchesTopThreeValues()
    {
        var sheet = new Worksheet(10, 10);
        sheet.Cells[0, 0].Value = 1;
        sheet.Cells[1, 0].Value = 2;
        sheet.Cells[2, 0].Value = 3;
        sheet.Cells[3, 0].Value = 4;
        sheet.Cells[4, 0].Value = 5;

        var range = new RangeRef(new CellRef(0, 0), new CellRef(4, 0));
        var rule = new Top10Rule { Count = 3, Bottom = false, Format = new Format { Bold = true } };
        sheet.ConditionalFormats.Add(range, rule);

        Assert.Null(sheet.ConditionalFormats.Calculate(sheet.Cells[0, 0]));
        Assert.Null(sheet.ConditionalFormats.Calculate(sheet.Cells[1, 0]));
        Assert.NotNull(sheet.ConditionalFormats.Calculate(sheet.Cells[2, 0]));
        Assert.NotNull(sheet.ConditionalFormats.Calculate(sheet.Cells[3, 0]));
        Assert.NotNull(sheet.ConditionalFormats.Calculate(sheet.Cells[4, 0]));
    }

    [Fact]
    public void Top10Rule_Bottom2_MatchesLowestTwoValues()
    {
        var sheet = new Worksheet(10, 10);
        sheet.Cells[0, 0].Value = 1;
        sheet.Cells[1, 0].Value = 2;
        sheet.Cells[2, 0].Value = 3;
        sheet.Cells[3, 0].Value = 4;
        sheet.Cells[4, 0].Value = 5;

        var range = new RangeRef(new CellRef(0, 0), new CellRef(4, 0));
        var rule = new Top10Rule { Count = 2, Bottom = true, Format = new Format { Italic = true } };
        sheet.ConditionalFormats.Add(range, rule);

        Assert.NotNull(sheet.ConditionalFormats.Calculate(sheet.Cells[0, 0]));
        Assert.NotNull(sheet.ConditionalFormats.Calculate(sheet.Cells[1, 0]));
        Assert.Null(sheet.ConditionalFormats.Calculate(sheet.Cells[2, 0]));
        Assert.Null(sheet.ConditionalFormats.Calculate(sheet.Cells[3, 0]));
        Assert.Null(sheet.ConditionalFormats.Calculate(sheet.Cells[4, 0]));
    }

    [Fact]
    public void Top10Rule_TiesAtThreshold_IncludeAllTiedValues()
    {
        var sheet = new Worksheet(10, 10);
        sheet.Cells[0, 0].Value = 5;
        sheet.Cells[1, 0].Value = 5;
        sheet.Cells[2, 0].Value = 5;
        sheet.Cells[3, 0].Value = 1;

        var range = new RangeRef(new CellRef(0, 0), new CellRef(3, 0));
        var rule = new Top10Rule { Count = 2, Bottom = false, Format = new Format { Bold = true } };
        sheet.ConditionalFormats.Add(range, rule);

        Assert.NotNull(sheet.ConditionalFormats.Calculate(sheet.Cells[0, 0]));
        Assert.NotNull(sheet.ConditionalFormats.Calculate(sheet.Cells[1, 0]));
        Assert.NotNull(sheet.ConditionalFormats.Calculate(sheet.Cells[2, 0]));
        Assert.NotNull(sheet.ConditionalFormats.Calculate(sheet.Cells[3, 0]));
    }

    [Fact]
    public void Top10Rule_IgnoresNonNumericCellsButFormatsOnlyMatchingNumerics()
    {
        var sheet = new Worksheet(10, 10);
        sheet.Cells[0, 0].Value = "text";
        sheet.Cells[1, 0].Value = 10;
        sheet.Cells[2, 0].Value = 20;
        sheet.Cells[3, 0].Value = 30;

        var range = new RangeRef(new CellRef(0, 0), new CellRef(3, 0));
        var rule = new Top10Rule { Count = 1, Bottom = false, Format = new Format { Bold = true } };
        sheet.ConditionalFormats.Add(range, rule);

        Assert.Null(sheet.ConditionalFormats.Calculate(sheet.Cells[0, 0]));
        Assert.Null(sheet.ConditionalFormats.Calculate(sheet.Cells[1, 0]));
        Assert.Null(sheet.ConditionalFormats.Calculate(sheet.Cells[2, 0]));
        Assert.NotNull(sheet.ConditionalFormats.Calculate(sheet.Cells[3, 0]));
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
        Assert.True(result?.Bold);

        command.Unexecute();

        // Verify rule was removed
        result = sheet.ConditionalFormats.Calculate(sheet.Cells[0, 0]);
        Assert.Null(result);
    }
}
