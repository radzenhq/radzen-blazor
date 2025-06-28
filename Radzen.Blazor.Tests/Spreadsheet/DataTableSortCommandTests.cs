using System;
using Xunit;

namespace Radzen.Blazor.Spreadsheet.Tests;

public class DataTableSortCommandTests
{
    [Fact]
    public void DataTableSortCommand_ShouldSortDataInAscendingOrder()
    {
        // Arrange
        var sheet = new Sheet(5, 5);
        var range = RangeRef.Parse("A1:A3");
        
        // Set up test data
        sheet.Cells["A1"].Value = "Charlie";
        sheet.Cells["A2"].Value = "Alice";
        sheet.Cells["A3"].Value = "Bob";
        
        var command = new DataTableSortCommand(sheet, range, SortOrder.Ascending, 0);
        
        // Act
        var result = command.Execute();
        
        // Assert
        Assert.True(result);
        Assert.Equal("Alice", sheet.Cells["A1"].Value);
        Assert.Equal("Bob", sheet.Cells["A2"].Value);
        Assert.Equal("Charlie", sheet.Cells["A3"].Value);
    }
    
    [Fact]
    public void DataTableSortCommand_ShouldSortDataInDescendingOrder()
    {
        // Arrange
        var sheet = new Sheet(5, 5);
        var range = RangeRef.Parse("A1:A3");
        
        // Set up test data
        sheet.Cells["A1"].Value = "Alice";
        sheet.Cells["A2"].Value = "Bob";
        sheet.Cells["A3"].Value = "Charlie";
        
        var command = new DataTableSortCommand(sheet, range, SortOrder.Descending, 0);
        
        // Act
        var result = command.Execute();
        
        // Assert
        Assert.True(result);
        Assert.Equal("Charlie", sheet.Cells["A1"].Value);
        Assert.Equal("Bob", sheet.Cells["A2"].Value);
        Assert.Equal("Alice", sheet.Cells["A3"].Value);
    }
    
    [Fact]
    public void DataTableSortCommand_ShouldRestoreOriginalOrderWhenUndone()
    {
        // Arrange
        var sheet = new Sheet(5, 5);
        var range = RangeRef.Parse("A1:A3");
        
        // Set up test data
        sheet.Cells["A1"].Value = "Charlie";
        sheet.Cells["A2"].Value = "Alice";
        sheet.Cells["A3"].Value = "Bob";
        
        var command = new DataTableSortCommand(sheet, range, SortOrder.Ascending, 0);
        
        // Act
        command.Execute();
        command.Unexecute();
        
        // Assert
        Assert.Equal("Charlie", sheet.Cells["A1"].Value);
        Assert.Equal("Alice", sheet.Cells["A2"].Value);
        Assert.Equal("Bob", sheet.Cells["A3"].Value);
    }
    
    [Fact]
    public void DataTableSortCommand_ShouldReturnFalseForInvalidRange()
    {
        // Arrange
        var sheet = new Sheet(5, 5);
        var command = new DataTableSortCommand(sheet, RangeRef.Invalid, SortOrder.Ascending, 0);
        
        // Act
        var result = command.Execute();
        
        // Assert
        Assert.False(result);
    }
    
    [Fact]
    public void DataTableSortCommand_ShouldPreserveCellFormatting()
    {
        // Arrange
        var sheet = new Sheet(5, 5);
        var range = RangeRef.Parse("A1:A2");
        
        // Set up test data with formatting
        sheet.Cells["A1"].Value = "Charlie";
        sheet.Cells["A1"].Format.Bold = true;
        sheet.Cells["A2"].Value = "Alice";
        sheet.Cells["A2"].Format.Italic = true;
        
        var command = new DataTableSortCommand(sheet, range, SortOrder.Ascending, 0);
        
        // Act
        command.Execute();
        command.Unexecute();
        
        // Assert
        Assert.Equal("Charlie", sheet.Cells["A1"].Value);
        Assert.True(sheet.Cells["A1"].Format.Bold);
        Assert.Equal("Alice", sheet.Cells["A2"].Value);
        Assert.True(sheet.Cells["A2"].Format.Italic);
    }
} 