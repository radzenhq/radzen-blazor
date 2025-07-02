using Xunit;

namespace Radzen.Blazor.Spreadsheet.Tests;

public class AutoFilterTests
{
    private readonly Sheet sheet = new(10, 10);

    [Fact]
    public void Should_ToggleSheetAutoFilter()
    {
        // Initially no auto filter
        Assert.Null(sheet.AutoFilter);

        // Apply auto filter to range A1:C5
        var range = RangeRef.Parse("A1:C5");
        var command = new SheetAutoFilterCommand(sheet, range);
        command.Execute();

        // Auto filter should be applied
        Assert.NotNull(sheet.AutoFilter);
        Assert.Equal(range, sheet.AutoFilter.Range);

        // Undo the command
        command.Unexecute();

        // Auto filter should be removed
        Assert.Null(sheet.AutoFilter);
    }

    [Fact]
    public void Should_ToggleDataTableFilterButton()
    {
        // Add a data table
        var range = RangeRef.Parse("A1:C5");
        sheet.AddTable(range);

        var table = sheet.Tables[0];
        
        // Initially ShowFilterButton should be true
        Assert.True(table.ShowFilterButton);

        // Toggle filter button off
        var command = new TableFilterCommand(sheet, 0);
        command.Execute();

        // ShowFilterButton should be false
        Assert.False(table.ShowFilterButton);

        // Undo the command
        command.Unexecute();

        // ShowFilterButton should be true again
        Assert.True(table.ShowFilterButton);
    }

    [Fact]
    public void Should_HandleMultipleDataTables()
    {
        // Add two data tables
        sheet.AddTable(RangeRef.Parse("A1:C5"));
        sheet.AddTable(RangeRef.Parse("E1:G5"));

        var table1 = sheet.Tables[0];
        var table2 = sheet.Tables[1];

        // Initially both should have ShowFilterButton = true
        Assert.True(table1.ShowFilterButton);
        Assert.True(table2.ShowFilterButton);

        // Toggle filter button for first data table
        var command1 = new TableFilterCommand(sheet, 0);
        command1.Execute();

        // Only first data table should be affected
        Assert.False(table1.ShowFilterButton);
        Assert.True(table2.ShowFilterButton);

        // Toggle filter button for second data table
        var command2 = new TableFilterCommand(sheet, 1);
        command2.Execute();

        // Both should be affected
        Assert.False(table1.ShowFilterButton);
        Assert.False(table2.ShowFilterButton);

        // Undo second command
        command2.Unexecute();

        // Only second data table should be restored
        Assert.False(table1.ShowFilterButton);
        Assert.True(table2.ShowFilterButton);
    }

    [Fact]
    public void Should_HandleInvalidDataTableIndex()
    {
        // Try to toggle filter button for non-existent data table
        var command = new TableFilterCommand(sheet, 0);
        
        // Should not throw exception
        var result = command.Execute();
        Assert.True(result);
    }
} 