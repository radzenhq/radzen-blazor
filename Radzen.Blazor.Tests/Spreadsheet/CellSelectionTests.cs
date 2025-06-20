using Bunit;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Radzen.Blazor.Spreadsheet.Tests;

public class CellSelectionTests : TestContext
{
    private readonly Sheet sheet = new (4,4);

    [Fact]
    public void CellSelection_RendersWithCorrectClasses()
    {
        // Arrange
        var cell = new CellRef(0, 0);
        sheet.Selection.Select(new RangeRef(cell, cell));
        var context = new MockVirtualGridContext();

        // Act
        var cut = RenderComponent<CellSelection>(parameters => parameters
            .Add(p => p.Cell, cell)
            .Add(p => p.Sheet, sheet)
            .Add(p => p.Context, context));

        // Assert
        var element = cut.Find(".rz-spreadsheet-selection-cell");
        Assert.NotNull(element);
        Assert.Contains("rz-spreadsheet-selection-cell", element.ClassName);
        Assert.Contains("rz-spreadsheet-selection-cell-top", element.ClassName);
        Assert.Contains("rz-spreadsheet-selection-cell-left", element.ClassName);
        Assert.Contains("rz-spreadsheet-selection-cell-bottom", element.ClassName);
        Assert.Contains("rz-spreadsheet-selection-cell-right", element.ClassName);
    }

    [Fact]
    public void CellSelection_AppliesFrozenColumnClass()
    {
        // Arrange
        var cell = new CellRef(0, 0);
        sheet.Columns.Frozen = 1;
        sheet.Selection.Select(new RangeRef(cell, cell));
        var context = new MockVirtualGridContext();

        // Act
        var cut = RenderComponent<CellSelection>(parameters => parameters
            .Add(p => p.Cell, cell)
            .Add(p => p.Sheet, sheet)
            .Add(p => p.Context, context));

        // Assert
        var element = cut.Find(".rz-spreadsheet-selection-cell");
        Assert.Contains("rz-spreadsheet-frozen-column", element.ClassName);
    }

    [Fact]
    public void CellSelection_AppliesFrozenRowClass()
    {
        // Arrange
        var cell = new CellRef(0, 0);
        var context = new MockVirtualGridContext();
        sheet.Rows.Frozen = 1;
        sheet.Selection.Select(new RangeRef(cell, cell));

        // Act
        var cut = RenderComponent<CellSelection>(parameters => parameters
            .Add(p => p.Cell, cell)
            .Add(p => p.Sheet, sheet)
            .Add(p => p.Context, context));

        // Assert
        var element = cut.Find(".rz-spreadsheet-selection-cell");
        Assert.Contains("rz-spreadsheet-frozen-row", element.ClassName);
    }

    [Fact]
    public void CellSelection_CalculatesStyle()
    {
        // Arrange
        var cell = new CellRef(0, 0);
        sheet.Selection.Select(new RangeRef(cell, cell));
        var context = new MockVirtualGridContext();

        // Act
        var cut = RenderComponent<CellSelection>(parameters => parameters
            .Add(p => p.Cell, cell)
            .Add(p => p.Sheet, sheet)
            .Add(p => p.Context, context));

        // Assert
        var element = cut.Find(".rz-spreadsheet-selection-cell");
        Assert.Equal("transform: translate(0px, 0px); width: 100px; height: 24px", element.GetAttribute("style"));
    }

    [Fact]
    public void CellSelection_SplitsMergedCell_WhenIntersectingFrozenRow()
    {
        // Arrange
        sheet.Rows.Frozen = 1;
        var range = new RangeRef(new CellRef(0, 0), new CellRef(2, 0));
        sheet.MergedCells.Add(range);
        sheet.Selection.Select(range);
        var context = new MockVirtualGridContext();

        // Act
        var cut = RenderComponent<CellSelection>(parameters => parameters
            .Add(p => p.Cell, new CellRef(0, 0))
            .Add(p => p.Sheet, sheet)
            .Add(p => p.Context, context));

        // Assert
        var elements = cut.FindAll(".rz-spreadsheet-selection-cell");
        Assert.Equal(2, elements.Count);

        var frozen = cut.Find(".rz-spreadsheet-frozen-row");

        Assert.NotNull(frozen);
        
        // First element (frozen)
        Assert.Equal("transform: translate(0px, 0px); width: 100px; height: 24px", frozen.GetAttribute("style"));
        Assert.Contains("rz-spreadsheet-selection-cell-top", frozen.ClassName);
        Assert.Contains("rz-spreadsheet-selection-cell-left", frozen.ClassName);
        Assert.Contains("rz-spreadsheet-selection-cell-right", frozen.ClassName);
        Assert.DoesNotContain("rz-spreadsheet-selection-cell-bottom", frozen.ClassName);

        var unfrozen = elements.Where(e => e != frozen).FirstOrDefault();

        Assert.NotNull(unfrozen);
        
        // Second element (non-frozen)
        Assert.Equal("transform: translate(0px, 24px); width: 100px; height: 48px", unfrozen.GetAttribute("style"));
        Assert.DoesNotContain("rz-spreadsheet-selection-cell-top", unfrozen.ClassName);
        Assert.Contains("rz-spreadsheet-selection-cell-left", unfrozen.ClassName);
        Assert.Contains("rz-spreadsheet-selection-cell-right", unfrozen.ClassName);
        Assert.Contains("rz-spreadsheet-selection-cell-bottom", unfrozen.ClassName);
    }

    [Fact]
    public void CellSelection_SplitsMergedCell_WhenIntersectingFrozenColumn()
    {
        // Arrange
        sheet.Columns.Frozen = 1;
        var range = new RangeRef(new CellRef(0, 0), new CellRef(0, 2));
        sheet.MergedCells.Add(range);
        sheet.Selection.Select(range);
        var context = new MockVirtualGridContext();

        // Act
        var cut = RenderComponent<CellSelection>(parameters => parameters
            .Add(p => p.Cell, new CellRef(0, 0))
            .Add(p => p.Sheet, sheet)
            .Add(p => p.Context, context));

        // Assert
        var elements = cut.FindAll(".rz-spreadsheet-selection-cell");
        Assert.Equal(2, elements.Count);

        var frozen = cut.Find(".rz-spreadsheet-frozen-column");

        Assert.NotNull(frozen);
        
        // First element (frozen)
        Assert.Contains("rz-spreadsheet-frozen-column", frozen.ClassName);
        Assert.Equal("transform: translate(0px, 0px); width: 100px; height: 24px", frozen.GetAttribute("style"));
        Assert.Contains("rz-spreadsheet-selection-cell-top", frozen.ClassName);
        Assert.Contains("rz-spreadsheet-selection-cell-left", frozen.ClassName);
        Assert.Contains("rz-spreadsheet-selection-cell-bottom", frozen.ClassName);
        Assert.DoesNotContain("rz-spreadsheet-selection-cell-right", frozen.ClassName);
        
        var unfrozen = elements.Where(e => e != frozen).FirstOrDefault();

        Assert.NotNull(unfrozen);
        // Second element (non-frozen)
        Assert.Equal("transform: translate(100px, 0px); width: 200px; height: 24px", unfrozen.GetAttribute("style"));
        Assert.Contains("rz-spreadsheet-selection-cell-top", unfrozen.ClassName);
        Assert.DoesNotContain("rz-spreadsheet-selection-cell-left", unfrozen.ClassName);
        Assert.Contains("rz-spreadsheet-selection-cell-bottom", unfrozen.ClassName);
        Assert.Contains("rz-spreadsheet-selection-cell-right", unfrozen.ClassName);
    }

    [Fact]
    public void CellSelection_SplitsMergedCell_WhenIntersectingBothFrozen()
    {
        // Arrange
        sheet.Rows.Frozen = 1;
        sheet.Columns.Frozen = 1;
        var range = new RangeRef(new CellRef(0, 0), new CellRef(2, 2));
        sheet.MergedCells.Add(range);
        sheet.Selection.Select(range);
        var context = new MockVirtualGridContext();

        // Act
        var cut = RenderComponent<CellSelection>(parameters => parameters
            .Add(p => p.Cell, new CellRef(0, 0))
            .Add(p => p.Sheet, sheet)
            .Add(p => p.Context, context));

        // Assert
        var elements = cut.FindAll(".rz-spreadsheet-selection-cell");
        Assert.Equal(4, elements.Count);

        // Top-left element (both frozen)
        var both = cut.Find(".rz-spreadsheet-frozen-row.rz-spreadsheet-frozen-column");
        Assert.NotNull(both);
        Assert.Contains("rz-spreadsheet-frozen-row", both.ClassName);
        Assert.Contains("rz-spreadsheet-frozen-column", both.ClassName);
        Assert.Equal("transform: translate(0px, 0px); width: 100px; height: 24px", both.GetAttribute("style"));
        Assert.Contains("rz-spreadsheet-selection-cell-top", both.ClassName);
        Assert.Contains("rz-spreadsheet-selection-cell-left", both.ClassName);
        Assert.DoesNotContain("rz-spreadsheet-selection-cell-bottom", both.ClassName);
        Assert.DoesNotContain("rz-spreadsheet-selection-cell-right", both.ClassName);
        
        // Bottom-left element (column frozen)
        var frozenColumn = cut.Find(".rz-spreadsheet-frozen-column:not(.rz-spreadsheet-frozen-row)");
        Assert.NotNull(frozenColumn);
        Assert.DoesNotContain("rz-spreadsheet-frozen-row", frozenColumn.ClassName);
        Assert.Contains("rz-spreadsheet-frozen-column", frozenColumn.ClassName);
        Assert.Equal("transform: translate(0px, 24px); width: 100px; height: 48px", frozenColumn.GetAttribute("style"));
        Assert.DoesNotContain("rz-spreadsheet-selection-cell-top", frozenColumn.ClassName);
        Assert.Contains("rz-spreadsheet-selection-cell-left", frozenColumn.ClassName);
        Assert.Contains("rz-spreadsheet-selection-cell-bottom", frozenColumn.ClassName);
        Assert.DoesNotContain("rz-spreadsheet-selection-cell-right", frozenColumn.ClassName);
        
        // Top-right element (row frozen)
        var frozenRow = cut.Find(".rz-spreadsheet-frozen-row:not(.rz-spreadsheet-frozen-column)");
        Assert.NotNull(frozenRow);
        Assert.Contains("rz-spreadsheet-frozen-row", frozenRow.ClassName);
        Assert.DoesNotContain("rz-spreadsheet-frozen-column", frozenRow.ClassName);
        Assert.Equal("transform: translate(100px, 0px); width: 200px; height: 24px", frozenRow.GetAttribute("style"));
        Assert.Contains("rz-spreadsheet-selection-cell-top", frozenRow.ClassName);
        Assert.DoesNotContain("rz-spreadsheet-selection-cell-left", frozenRow.ClassName);
        Assert.DoesNotContain("rz-spreadsheet-selection-cell-bottom", frozenRow.ClassName);
        Assert.Contains("rz-spreadsheet-selection-cell-right", frozenRow.ClassName);
        
        // Bottom-right element (neither frozen)
        var neither = elements.FirstOrDefault(e => e != both && e != frozenColumn && e != frozenRow);
        Assert.NotNull(neither);
        Assert.DoesNotContain("rz-spreadsheet-frozen-row", neither.ClassName);
        Assert.DoesNotContain("rz-spreadsheet-frozen-column", neither.ClassName);
        Assert.Equal("transform: translate(100px, 24px); width: 200px; height: 48px", neither.GetAttribute("style"));
        Assert.DoesNotContain("rz-spreadsheet-selection-cell-top", neither.ClassName);
        Assert.DoesNotContain("rz-spreadsheet-selection-cell-left", neither.ClassName);
        Assert.Contains("rz-spreadsheet-selection-cell-bottom", neither.ClassName);
        Assert.Contains("rz-spreadsheet-selection-cell-right", neither.ClassName);
    }
}

public class MockVirtualGridContext : IVirtualGridContext
{
    private readonly Dictionary<(int Row, int Column), PixelRectangle> rectangle = [];

    public void SetupRectangle(int row, int column, PixelRectangle rectangle)
    {
        this.rectangle[(row, column)] = rectangle;
    }

    public PixelRectangle GetRectangle(int row, int column) => throw new NotImplementedException();

    public PixelRectangle GetRectangle(int top, int left, int bottom, int right) => new(new (left * 100, (right + 1) * 100), new (top*24, (bottom + 1)*24));
} 