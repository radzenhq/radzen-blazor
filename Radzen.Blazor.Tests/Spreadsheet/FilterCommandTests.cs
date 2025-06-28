using System;
using Xunit;

namespace Radzen.Blazor.Spreadsheet.Tests;

public class FilterCommandTests
{
    private readonly Sheet sheet = new(10, 10);

    [Fact]
    public void Should_AddFilterWithCommand()
    {
        // Initially no filters
        Assert.Empty(sheet.Filters);

        // Create a filter
        var filter = new SheetFilter(
            new EqualsCriterion { Column = 0, Value = "Test" },
            RangeRef.Parse("A1:A5")
        );

        // Execute the command
        var command = new FilterCommand(sheet, filter);
        var result = command.Execute();

        // Command should succeed
        Assert.True(result);

        // Filter should be added
        Assert.Single(sheet.Filters);
        Assert.Contains(filter, sheet.Filters);
    }

    [Fact]
    public void Should_UndoFilterCommand()
    {
        // Initially no filters
        Assert.Empty(sheet.Filters);

        // Create a filter
        var filter = new SheetFilter(
            new EqualsCriterion { Column = 0, Value = "Test" },
            RangeRef.Parse("A1:A5")
        );

        // Execute the command
        var command = new FilterCommand(sheet, filter);
        command.Execute();

        // Filter should be added
        Assert.Single(sheet.Filters);

        // Undo the command
        command.Unexecute();

        // Filter should be removed
        Assert.Empty(sheet.Filters);
    }

    [Fact]
    public void Should_WorkWithUndoRedoStack()
    {
        // Initially no filters
        Assert.Empty(sheet.Filters);

        // Create a filter
        var filter = new SheetFilter(
            new EqualsCriterion { Column = 0, Value = "Test" },
            RangeRef.Parse("A1:A5")
        );

        // Execute the command through the undo/redo stack
        var command = new FilterCommand(sheet, filter);
        var result = sheet.Commands.Execute(command);

        // Command should succeed
        Assert.True(result);

        // Filter should be added
        Assert.Single(sheet.Filters);

        // Undo should be available
        Assert.True(sheet.Commands.CanUndo);

        // Undo the command
        sheet.Commands.Undo();

        // Filter should be removed
        Assert.Empty(sheet.Filters);

        // Redo should be available
        Assert.True(sheet.Commands.CanRedo);

        // Redo the command
        sheet.Commands.Redo();

        // Filter should be added again
        Assert.Single(sheet.Filters);
    }

    [Fact]
    public void Should_HandleMultipleFilters()
    {
        // Initially no filters
        Assert.Empty(sheet.Filters);

        // Create multiple filters
        var filter1 = new SheetFilter(
            new EqualsCriterion { Column = 0, Value = "Test1" },
            RangeRef.Parse("A1:A5")
        );

        var filter2 = new SheetFilter(
            new EqualsCriterion { Column = 1, Value = "Test2" },
            RangeRef.Parse("B1:B5")
        );

        // Execute commands through the undo/redo stack
        var command1 = new FilterCommand(sheet, filter1);
        var command2 = new FilterCommand(sheet, filter2);

        sheet.Commands.Execute(command1);
        sheet.Commands.Execute(command2);

        // Both filters should be added
        Assert.Equal(2, sheet.Filters.Count);
        Assert.Contains(filter1, sheet.Filters);
        Assert.Contains(filter2, sheet.Filters);

        // Undo both commands
        sheet.Commands.Undo(); // Undo filter2
        sheet.Commands.Undo(); // Undo filter1

        // No filters should remain
        Assert.Empty(sheet.Filters);

        // Redo both commands
        sheet.Commands.Redo(); // Redo filter1
        sheet.Commands.Redo(); // Redo filter2

        // Both filters should be back
        Assert.Equal(2, sheet.Filters.Count);
        Assert.Contains(filter1, sheet.Filters);
        Assert.Contains(filter2, sheet.Filters);
    }
}