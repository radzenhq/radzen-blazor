using Radzen;
using Radzen.Documents.Spreadsheet;
using Xunit;

namespace Radzen.Blazor.Spreadsheet.Tests;

#nullable enable

// Contract for the multi-key sort API: SortKey + SortOn enum + new
// Worksheet.Sort(range, params SortKey[]) overload. Column indices in
// SortKey are relative to the range's left edge.
public class MultiKeySortContractTests
{
    private static (Workbook wb, Worksheet ws) Build(int rows, int cols)
    {
        var wb = new Workbook();
        var ws = wb.AddSheet("Sheet1", rows, cols);
        return (wb, ws);
    }

    private static RangeRef Range(int r1, int c1, int r2, int c2) =>
        new(new CellRef(r1, c1), new CellRef(r2, c2));

    // ── SortKey shape ───────────────────────────────────────────────────────
    [Fact]
    public void SortKey_DefaultsAreSensible()
    {
        var key = new SortKey { ColumnIndex = 0 };
        Assert.Equal(SortOrder.Ascending, key.Order);
        Assert.Equal(SortOn.Values, key.SortOn);
        Assert.False(key.CaseSensitive);
        Assert.Null(key.CustomList);
    }

    // ── Multi-key sort ──────────────────────────────────────────────────────
    [Fact]
    public void Sort_TwoKeys_ShouldUseSecondaryWhenPrimaryEquals()
    {
        // Region asc, then Q1 desc
        var (_, ws) = Build(4, 2);
        ws.Cells[0, 0].SetValue("EMEA"); ws.Cells[0, 1].Value = 100.0;
        ws.Cells[1, 0].SetValue("AMER"); ws.Cells[1, 1].Value = 50.0;
        ws.Cells[2, 0].SetValue("EMEA"); ws.Cells[2, 1].Value = 200.0;
        ws.Cells[3, 0].SetValue("AMER"); ws.Cells[3, 1].Value = 75.0;

        ws.Sort(Range(0, 0, 3, 1),
            new SortKey { ColumnIndex = 0, Order = SortOrder.Ascending },
            new SortKey { ColumnIndex = 1, Order = SortOrder.Descending });

        // Expected order: AMER 75, AMER 50, EMEA 200, EMEA 100
        Assert.Equal("AMER", ws.Cells[0, 0].Value); Assert.Equal(75.0, ws.Cells[0, 1].Value);
        Assert.Equal("AMER", ws.Cells[1, 0].Value); Assert.Equal(50.0, ws.Cells[1, 1].Value);
        Assert.Equal("EMEA", ws.Cells[2, 0].Value); Assert.Equal(200.0, ws.Cells[2, 1].Value);
        Assert.Equal("EMEA", ws.Cells[3, 0].Value); Assert.Equal(100.0, ws.Cells[3, 1].Value);
    }

    [Fact]
    public void Sort_SingleKey_ShouldBeRelativeToRange()
    {
        // Sort B1:D3 by column index 0 (which is sheet column B, the FIRST column of the range)
        var (_, ws) = Build(3, 4);
        // A column is unrelated (left of range)
        ws.Cells[0, 0].SetValue("a"); ws.Cells[1, 0].SetValue("b"); ws.Cells[2, 0].SetValue("c");
        // B column = sort key
        ws.Cells[0, 1].Value = 30.0;
        ws.Cells[1, 1].Value = 10.0;
        ws.Cells[2, 1].Value = 20.0;
        ws.Cells[0, 2].SetValue("X"); ws.Cells[1, 2].SetValue("Y"); ws.Cells[2, 2].SetValue("Z");

        ws.Sort(Range(0, 1, 2, 3), new SortKey { ColumnIndex = 0 });

        // A column untouched
        Assert.Equal("a", ws.Cells[0, 0].Value);
        // B column sorted ascending: 10, 20, 30
        Assert.Equal(10.0, ws.Cells[0, 1].Value);
        Assert.Equal(20.0, ws.Cells[1, 1].Value);
        Assert.Equal(30.0, ws.Cells[2, 1].Value);
        // C column moves with B
        Assert.Equal("Y", ws.Cells[0, 2].Value);
        Assert.Equal("Z", ws.Cells[1, 2].Value);
        Assert.Equal("X", ws.Cells[2, 2].Value);
    }

    // ── Custom list ─────────────────────────────────────────────────────────
    [Fact]
    public void Sort_WithCustomList_ShouldUseListOrder()
    {
        var (_, ws) = Build(4, 1);
        ws.Cells[0, 0].SetValue("Mar");
        ws.Cells[1, 0].SetValue("Jan");
        ws.Cells[2, 0].SetValue("Feb");
        ws.Cells[3, 0].SetValue("Apr");

        ws.Sort(Range(0, 0, 3, 0),
            new SortKey { ColumnIndex = 0, CustomList = ["Jan", "Feb", "Mar", "Apr"] });

        Assert.Equal("Jan", ws.Cells[0, 0].Value);
        Assert.Equal("Feb", ws.Cells[1, 0].Value);
        Assert.Equal("Mar", ws.Cells[2, 0].Value);
        Assert.Equal("Apr", ws.Cells[3, 0].Value);
    }

    [Fact]
    public void Sort_WithCustomList_DescendingShouldReverse()
    {
        var (_, ws) = Build(3, 1);
        ws.Cells[0, 0].SetValue("Feb");
        ws.Cells[1, 0].SetValue("Jan");
        ws.Cells[2, 0].SetValue("Mar");

        ws.Sort(Range(0, 0, 2, 0),
            new SortKey
            {
                ColumnIndex = 0,
                Order = SortOrder.Descending,
                CustomList = ["Jan", "Feb", "Mar"],
            });

        Assert.Equal("Mar", ws.Cells[0, 0].Value);
        Assert.Equal("Feb", ws.Cells[1, 0].Value);
        Assert.Equal("Jan", ws.Cells[2, 0].Value);
    }

    [Fact]
    public void Sort_WithCustomList_ValuesNotInListShouldComeAfter()
    {
        var (_, ws) = Build(4, 1);
        ws.Cells[0, 0].SetValue("Apple");
        ws.Cells[1, 0].SetValue("Jan");
        ws.Cells[2, 0].SetValue("Banana");
        ws.Cells[3, 0].SetValue("Feb");

        ws.Sort(Range(0, 0, 3, 0),
            new SortKey { ColumnIndex = 0, CustomList = ["Jan", "Feb"] });

        // Items in the list come first, in list order
        Assert.Equal("Jan", ws.Cells[0, 0].Value);
        Assert.Equal("Feb", ws.Cells[1, 0].Value);
        // Items not in the list follow, alphabetically
        Assert.Equal("Apple", ws.Cells[2, 0].Value);
        Assert.Equal("Banana", ws.Cells[3, 0].Value);
    }

    // ── Case sensitivity ────────────────────────────────────────────────────
    [Fact]
    public void Sort_CaseInsensitive_ShouldTreatVariantsAsEqual_AndPreserveOrder()
    {
        var (_, ws) = Build(3, 2);
        ws.Cells[0, 0].SetValue("apple"); ws.Cells[0, 1].Value = 1.0;
        ws.Cells[1, 0].SetValue("Apple"); ws.Cells[1, 1].Value = 2.0;
        ws.Cells[2, 0].SetValue("APPLE"); ws.Cells[2, 1].Value = 3.0;

        ws.Sort(Range(0, 0, 2, 1), new SortKey { ColumnIndex = 0 });
        // Stable: original order preserved when keys are equal
        Assert.Equal(1.0, ws.Cells[0, 1].Value);
        Assert.Equal(2.0, ws.Cells[1, 1].Value);
        Assert.Equal(3.0, ws.Cells[2, 1].Value);
    }

    [Fact]
    public void Sort_CaseSensitive_ShouldOrderUppercaseSeparately()
    {
        var (_, ws) = Build(3, 1);
        ws.Cells[0, 0].SetValue("apple");
        ws.Cells[1, 0].SetValue("Apple");
        ws.Cells[2, 0].SetValue("APPLE");

        ws.Sort(Range(0, 0, 2, 0),
            new SortKey { ColumnIndex = 0, CaseSensitive = true });

        // Ordinal sort: 'A' (0x41) < 'a' (0x61), so APPLE < Apple < apple.
        Assert.Equal("APPLE", ws.Cells[0, 0].Value);
        Assert.Equal("Apple", ws.Cells[1, 0].Value);
        Assert.Equal("apple", ws.Cells[2, 0].Value);
    }

    // ── Sort by color ───────────────────────────────────────────────────────
    [Fact]
    public void Sort_ByCellColor_ShouldGroupMatchingColorFirst()
    {
        var (_, ws) = Build(4, 1);
        ws.Cells[0, 0].SetValue("a");
        ws.Cells[1, 0].SetValue("b"); ws.Cells[1, 0].Format = new Format { BackgroundColor = "#FFFF00" };
        ws.Cells[2, 0].SetValue("c");
        ws.Cells[3, 0].SetValue("d"); ws.Cells[3, 0].Format = new Format { BackgroundColor = "#FFFF00" };

        ws.Sort(Range(0, 0, 3, 0),
            new SortKey
            {
                ColumnIndex = 0,
                SortOn = SortOn.CellColor,
                CustomColor = "#FFFF00",
                Order = SortOrder.Ascending,
            });

        // Yellow rows come first (preserving their relative order).
        Assert.Equal("b", ws.Cells[0, 0].Value);
        Assert.Equal("d", ws.Cells[1, 0].Value);
        Assert.Equal("a", ws.Cells[2, 0].Value);
        Assert.Equal("c", ws.Cells[3, 0].Value);
    }

    // ── Empty + no-op cases ─────────────────────────────────────────────────
    [Fact]
    public void Sort_EmptyKeys_ShouldBeNoOp()
    {
        var (_, ws) = Build(3, 1);
        ws.Cells[0, 0].Value = 3.0;
        ws.Cells[1, 0].Value = 1.0;
        ws.Cells[2, 0].Value = 2.0;

        ws.Sort(Range(0, 0, 2, 0)); // params, no keys

        // Order unchanged
        Assert.Equal(3.0, ws.Cells[0, 0].Value);
        Assert.Equal(1.0, ws.Cells[1, 0].Value);
        Assert.Equal(2.0, ws.Cells[2, 0].Value);
    }

    [Fact]
    public void Sort_KeyOutsideRange_ShouldThrow()
    {
        var (_, ws) = Build(3, 2);
        Assert.Throws<System.ArgumentOutOfRangeException>(() =>
            ws.Sort(Range(0, 0, 2, 1), new SortKey { ColumnIndex = 5 }));
    }
}
