using Bunit;
using System.Linq;
using Xunit;

namespace Radzen.Blazor.Spreadsheet.Tests;

public class RangeSelectionItemTests : TestContext
{
    private readonly Sheet sheet = new(4, 4);

    [Fact]
    public void RangeSelectionItem_ShouldCalculateCorrectMaskForMergedCells()
    {
        // Arrange
        var mergedRange = RangeRef.Parse("B1:C1");
        sheet.MergedCells.Add(mergedRange);
        
        // Select a range that overlaps with the merged cell
        var selectionRange = RangeRef.Parse("A1:D1");
        sheet.Selection.Select(selectionRange);
        
        var context = new MockVirtualGridContext();

        // Act
        var cut = RenderComponent<RangeSelectionItem>(parameters => parameters
            .Add(p => p.Range, selectionRange)
            .Add(p => p.Sheet, sheet)
            .Add(p => p.Cell, sheet.Selection.Cell) // This should be A1 (the active cell)
            .Add(p => p.Context, context));

        // Assert
        var element = cut.Find(".rz-spreadsheet-selection-range");
        Assert.NotNull(element);
        
        // The style should include mask properties that account for the merged cell
        var style = element.GetAttribute("style");
        Assert.NotNull(style);
        Assert.Contains("mask-size", style);
        Assert.Contains("mask-position", style);
    }

    [Fact]
    public void RangeSelectionItem_ShouldHandleNonMergedCellsCorrectly()
    {
        // Arrange
        var selectionRange = RangeRef.Parse("A1:B2");
        sheet.Selection.Select(selectionRange);
        
        var context = new MockVirtualGridContext();

        // Act
        var cut = RenderComponent<RangeSelectionItem>(parameters => parameters
            .Add(p => p.Range, selectionRange)
            .Add(p => p.Sheet, sheet)
            .Add(p => p.Cell, sheet.Selection.Cell)
            .Add(p => p.Context, context));

        // Assert
        var element = cut.Find(".rz-spreadsheet-selection-range");
        Assert.NotNull(element);
        
        var style = element.GetAttribute("style");
        Assert.NotNull(style);
        Assert.Contains("transform", style);
        Assert.Contains("width", style);
        Assert.Contains("height", style);
    }

    [Fact]
    public void RangeSelectionItem_ShouldHandleMergedCellsAcrossFrozenColumnBoundary()
    {
        // Arrange
        sheet.Columns.Frozen = 1; // Freeze first column
        var mergedRange = RangeRef.Parse("A1:B1"); // Merged cell spans across frozen boundary
        sheet.MergedCells.Add(mergedRange);
        
        // Select the merged cell
        sheet.Selection.Select(mergedRange);
        
        var context = new MockVirtualGridContext();

        // Get the split ranges (frozen and non-frozen parts)
        var ranges = sheet.GetRanges(mergedRange).ToList();
        Assert.Equal(2, ranges.Count); // Should be split into 2 parts

        // Test the frozen part (A1:A1)
        var frozenRange = ranges.First(r => r.FrozenColumn);
        var frozenCut = RenderComponent<RangeSelectionItem>(parameters => parameters
            .Add(p => p.Range, frozenRange.Range)
            .Add(p => p.Sheet, sheet)
            .Add(p => p.Cell, sheet.Selection.Cell)
            .Add(p => p.Context, context)
            .Add(p => p.FrozenColumn, frozenRange.FrozenColumn)
            .Add(p => p.FrozenRow, frozenRange.FrozenRow)
            .Add(p => p.Top, frozenRange.Top)
            .Add(p => p.Left, frozenRange.Left)
            .Add(p => p.Bottom, frozenRange.Bottom)
            .Add(p => p.Right, frozenRange.Right));

        var frozenElement = frozenCut.Find(".rz-spreadsheet-selection-range");
        Assert.NotNull(frozenElement);
        Assert.Contains("rz-spreadsheet-frozen-column", frozenElement.ClassName);
        
        var frozenStyle = frozenElement.GetAttribute("style");
        Assert.NotNull(frozenStyle);
        Assert.Contains("mask-size", frozenStyle);
        Assert.Contains("mask-position", frozenStyle);

        // Test the non-frozen part (B1:B1)
        var nonFrozenRange = ranges.First(r => !r.FrozenColumn);
        var nonFrozenCut = RenderComponent<RangeSelectionItem>(parameters => parameters
            .Add(p => p.Range, nonFrozenRange.Range)
            .Add(p => p.Sheet, sheet)
            .Add(p => p.Cell, sheet.Selection.Cell)
            .Add(p => p.Context, context)
            .Add(p => p.FrozenColumn, nonFrozenRange.FrozenColumn)
            .Add(p => p.FrozenRow, nonFrozenRange.FrozenRow)
            .Add(p => p.Top, nonFrozenRange.Top)
            .Add(p => p.Left, nonFrozenRange.Left)
            .Add(p => p.Bottom, nonFrozenRange.Bottom)
            .Add(p => p.Right, nonFrozenRange.Right));

        var nonFrozenElement = nonFrozenCut.Find(".rz-spreadsheet-selection-range");
        Assert.NotNull(nonFrozenElement);
        Assert.DoesNotContain("rz-spreadsheet-frozen-column", nonFrozenElement.ClassName);
        
        var nonFrozenStyle = nonFrozenElement.GetAttribute("style");
        Assert.NotNull(nonFrozenStyle);
        Assert.Contains("mask-size", nonFrozenStyle);
        Assert.Contains("mask-position", nonFrozenStyle);
    }
} 