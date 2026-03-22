using Xunit;

using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet.Tests;

public class CellStoreEvictionTests
{
    // --- TryGet does not auto-create ---

    [Fact]
    public void TryGet_ReturnsFalse_ForUnpopulatedCell()
    {
        var sheet = new Worksheet(10, 10);

        Assert.False(sheet.Cells.TryGet(5, 5, out _));
        Assert.Equal(0, sheet.Cells.PopulatedCount);
    }

    [Fact]
    public void TryGet_ReturnsTrue_ForPopulatedCell()
    {
        var sheet = new Worksheet(10, 10);
        sheet.Cells[3, 3].Value = "hello";

        Assert.True(sheet.Cells.TryGet(3, 3, out var cell));
        Assert.Equal("hello", cell.Value);
    }

    [Fact]
    public void TryGet_ReturnsFalse_ForOutOfBounds()
    {
        var sheet = new Worksheet(5, 5);

        Assert.False(sheet.Cells.TryGet(10, 10, out _));
        Assert.False(sheet.Cells.TryGet(-1, 0, out _));
    }

    [Fact]
    public void TryGet_DoesNotGrowDictionary()
    {
        var sheet = new Worksheet(100, 100);

        for (int i = 0; i < 50; i++)
        {
            sheet.Cells.TryGet(i, i, out _);
        }

        Assert.Equal(0, sheet.Cells.PopulatedCount);
    }

    [Fact]
    public void Indexer_StillAutoCreates()
    {
        var sheet = new Worksheet(10, 10);

        var cell = sheet.Cells[5, 5];

        Assert.NotNull(cell);
        Assert.Equal(1, sheet.Cells.PopulatedCount);
    }

    // --- Cell.IsEmpty ---

    [Fact]
    public void IsEmpty_TrueForNewCell()
    {
        var sheet = new Worksheet(5, 5);
        var cell = sheet.Cells[0, 0];

        Assert.True(cell.IsEmpty);
    }

    [Fact]
    public void IsEmpty_FalseWhenValueSet()
    {
        var sheet = new Worksheet(5, 5);
        var cell = sheet.Cells[0, 0];
        cell.Value = "data";

        Assert.False(cell.IsEmpty);
    }

    [Fact]
    public void IsEmpty_FalseWhenFormulaSet()
    {
        var sheet = new Worksheet(5, 5);
        sheet.Cells[0, 0].Value = 1;
        var cell = sheet.Cells[1, 0];
        cell.Formula = "=A1+1";

        Assert.False(cell.IsEmpty);
    }

    [Fact]
    public void IsEmpty_FalseWhenFormatSet()
    {
        var sheet = new Worksheet(5, 5);
        var cell = sheet.Cells[0, 0];
        cell.Format = new Format { Bold = true };

        Assert.False(cell.IsEmpty);
    }

    [Fact]
    public void IsEmpty_FalseWhenHyperlinkSet()
    {
        var sheet = new Worksheet(5, 5);
        var cell = sheet.Cells[0, 0];
        cell.Hyperlink = new Hyperlink { Url = "https://example.com" };

        Assert.False(cell.IsEmpty);
    }

    [Fact]
    public void IsEmpty_TrueAfterClearingValue()
    {
        var sheet = new Worksheet(5, 5);
        var cell = sheet.Cells[0, 0];
        cell.Value = "data";
        cell.Value = null;

        Assert.True(cell.IsEmpty);
    }

    // --- CellStore.Compact ---

    [Fact]
    public void Compact_RemovesEmptyCells()
    {
        var sheet = new Worksheet(10, 10);

        // Auto-create several cells via indexer
        _ = sheet.Cells[0, 0];
        _ = sheet.Cells[1, 1];
        _ = sheet.Cells[2, 2];

        Assert.Equal(3, sheet.Cells.PopulatedCount);

        var removed = sheet.Cells.Compact();

        Assert.Equal(3, removed);
        Assert.Equal(0, sheet.Cells.PopulatedCount);
    }

    [Fact]
    public void Compact_KeepsCellsWithValues()
    {
        var sheet = new Worksheet(10, 10);
        sheet.Cells[0, 0].Value = "keep";
        _ = sheet.Cells[1, 1]; // empty
        sheet.Cells[2, 2].Value = "also keep";

        var removed = sheet.Cells.Compact();

        Assert.Equal(1, removed);
        Assert.Equal(2, sheet.Cells.PopulatedCount);
        Assert.True(sheet.Cells.HasCell(0, 0));
        Assert.False(sheet.Cells.HasCell(1, 1));
        Assert.True(sheet.Cells.HasCell(2, 2));
    }

    [Fact]
    public void Compact_KeepsCellsWithFormulas()
    {
        var sheet = new Worksheet(5, 5);
        sheet.Cells[0, 0].Value = 1;
        sheet.Cells[1, 0].Formula = "=A1+1";

        sheet.Cells.Compact();

        Assert.True(sheet.Cells.HasCell(0, 0));
        Assert.True(sheet.Cells.HasCell(1, 0));
    }

    [Fact]
    public void Compact_KeepsCellsWithFormat()
    {
        var sheet = new Worksheet(5, 5);
        sheet.Cells[0, 0].Format = new Format { Bold = true };
        _ = sheet.Cells[1, 0]; // empty

        sheet.Cells.Compact();

        Assert.True(sheet.Cells.HasCell(0, 0));
        Assert.False(sheet.Cells.HasCell(1, 0));
    }

    [Fact]
    public void Compact_KeepsCellsWithHyperlinks()
    {
        var sheet = new Worksheet(5, 5);
        sheet.Cells[0, 0].Hyperlink = new Hyperlink { Url = "https://example.com" };

        sheet.Cells.Compact();

        Assert.True(sheet.Cells.HasCell(0, 0));
    }

    [Fact]
    public void Compact_ReturnsZeroWhenNothingToRemove()
    {
        var sheet = new Worksheet(5, 5);
        sheet.Cells[0, 0].Value = "data";

        var removed = sheet.Cells.Compact();

        Assert.Equal(0, removed);
    }

    [Fact]
    public void Compact_ReturnsZeroOnEmptyStore()
    {
        var sheet = new Worksheet(5, 5);

        var removed = sheet.Cells.Compact();

        Assert.Equal(0, removed);
    }

    // --- Formula evaluation with unpopulated cells ---

    [Fact]
    public void FormulaReferencingEmptyCell_ReturnsZero()
    {
        var sheet = new Worksheet(5, 5);
        // A1 is empty (never accessed via indexer)
        sheet.Cells[1, 0].Formula = "=A1+10";

        Assert.Equal(10d, sheet.Cells[1, 0].Value);
    }

    [Fact]
    public void SumOverEmptyRange_ReturnsZero()
    {
        var sheet = new Worksheet(10, 10);
        // A1:A5 are all empty
        sheet.Cells[0, 1].Formula = "=SUM(A1:A5)";

        Assert.Equal(0d, sheet.Cells[0, 1].Value);
    }

    [Fact]
    public void SumOverMixedRange_IgnoresEmptyCells()
    {
        var sheet = new Worksheet(10, 10);
        sheet.Cells[0, 0].Value = 10d;
        // A2 is empty
        sheet.Cells[2, 0].Value = 30d;
        sheet.Cells[0, 1].Formula = "=SUM(A1:A3)";

        Assert.Equal(40d, sheet.Cells[0, 1].Value);
    }

    // --- Integration: operations don't bloat dictionary ---

    [Fact]
    public void GetDelimitedString_DoesNotCreateCells()
    {
        var sheet = new Worksheet(100, 100);
        sheet.Cells[0, 0].Value = "only";
        var countBefore = sheet.Cells.PopulatedCount;

        sheet.GetDelimitedString(new RangeRef(new CellRef(0, 0), new CellRef(9, 9)));

        Assert.Equal(countBefore, sheet.Cells.PopulatedCount);
    }

    [Fact]
    public void FormulaEvaluation_DependencyCellsCanBeCompacted()
    {
        var sheet = new Worksheet(100, 100);
        sheet.Cells[0, 0].Formula = "=B1+C1+D1";

        // The dependency graph creates cells for B1, C1, D1 via the indexer.
        // But they're empty and can be compacted away.
        var removed = sheet.Cells.Compact();

        Assert.True(removed >= 3); // B1, C1, D1 are empty
        Assert.False(sheet.Cells.HasCell(0, 1)); // B1 evicted
        Assert.False(sheet.Cells.HasCell(0, 2)); // C1 evicted
        Assert.False(sheet.Cells.HasCell(0, 3)); // D1 evicted
        Assert.True(sheet.Cells.HasCell(0, 0));  // formula cell kept
    }
}
