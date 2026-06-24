using Radzen.Documents.Spreadsheet;
using Xunit;

namespace Radzen.Blazor.Spreadsheet.Tests;

#nullable enable

public class CellMenuAvailableValuesTests
{
    private static (Workbook wb, Worksheet ws) Build()
    {
        var wb = new Workbook();
        var ws = wb.AddSheet("Sheet1", 6, 2);
        ws.Cells[0, 0].SetValue("Region");
        ws.Cells[0, 1].SetValue("Status");

        ws.Cells[1, 0].SetValue("EMEA");
        ws.Cells[1, 1].SetValue("Open");

        ws.Cells[2, 0].SetValue("EMEA");
        ws.Cells[2, 1].SetValue("Closed");

        ws.Cells[3, 0].SetValue("AMER");
        ws.Cells[3, 1].SetValue("Open");

        ws.Cells[4, 0].SetValue("AMER");
        ws.Cells[4, 1].SetValue("Closed");

        ws.Cells[5, 0].SetValue("APAC");
        ws.Cells[5, 1].SetValue("Open");

        ws.AutoFilter.Range = new RangeRef(new CellRef(0, 0), new CellRef(5, 1));
        return (wb, ws);
    }

    [Fact]
    public void IsRowHiddenByOtherColumnFilter_NoFilters_ReturnsFalseForAllRows()
    {
        var (_, ws) = Build();

        for (var r = 1; r <= 5; r++)
        {
            Assert.False(CellMenu.IsRowHiddenByOtherColumnFilter(ws, r, 0));
        }
    }

    [Fact]
    public void IsRowHiddenByOtherColumnFilter_OnlyCurrentColumnFiltered_ReturnsFalse()
    {
        var (_, ws) = Build();

        ws.AddFilter(new SheetFilter(
            new InListCriterion { Column = 0, Values = ["EMEA"] },
            new RangeRef(new CellRef(0, 0), new CellRef(5, 0))));

        // When asking about column 0, the only filter targets column 0 — every row contributes
        for (var r = 1; r <= 5; r++)
        {
            Assert.False(CellMenu.IsRowHiddenByOtherColumnFilter(ws, r, 0));
        }
    }

    [Fact]
    public void IsRowHiddenByOtherColumnFilter_OtherColumnFiltered_HidesRowsNotMatchingThatFilter()
    {
        var (_, ws) = Build();

        // Filter on column 1 (Status) = "Open"
        ws.AddFilter(new SheetFilter(
            new InListCriterion { Column = 1, Values = ["Open"] },
            new RangeRef(new CellRef(0, 1), new CellRef(5, 1))));

        // Asking about column 0 (Region): row should be hidden iff Status != Open
        Assert.False(CellMenu.IsRowHiddenByOtherColumnFilter(ws, 1, 0)); // EMEA / Open
        Assert.True(CellMenu.IsRowHiddenByOtherColumnFilter(ws, 2, 0));  // EMEA / Closed
        Assert.False(CellMenu.IsRowHiddenByOtherColumnFilter(ws, 3, 0)); // AMER / Open
        Assert.True(CellMenu.IsRowHiddenByOtherColumnFilter(ws, 4, 0));  // AMER / Closed
        Assert.False(CellMenu.IsRowHiddenByOtherColumnFilter(ws, 5, 0)); // APAC / Open
    }

    [Fact]
    public void IsRowHiddenByOtherColumnFilter_BothColumnsFiltered_IgnoresCurrentColumnFilter()
    {
        var (_, ws) = Build();

        // Filter on column 0 (Region) = EMEA
        ws.AddFilter(new SheetFilter(
            new InListCriterion { Column = 0, Values = ["EMEA"] },
            new RangeRef(new CellRef(0, 0), new CellRef(5, 0))));

        // Filter on column 1 (Status) = Open
        ws.AddFilter(new SheetFilter(
            new InListCriterion { Column = 1, Values = ["Open"] },
            new RangeRef(new CellRef(0, 1), new CellRef(5, 1))));

        // Asking about column 0: only the column-1 filter should be honored.
        // Rows with Status=Open are kept regardless of Region.
        Assert.False(CellMenu.IsRowHiddenByOtherColumnFilter(ws, 1, 0)); // EMEA / Open
        Assert.True(CellMenu.IsRowHiddenByOtherColumnFilter(ws, 2, 0));  // EMEA / Closed
        Assert.False(CellMenu.IsRowHiddenByOtherColumnFilter(ws, 3, 0)); // AMER / Open
        Assert.True(CellMenu.IsRowHiddenByOtherColumnFilter(ws, 4, 0));  // AMER / Closed
        Assert.False(CellMenu.IsRowHiddenByOtherColumnFilter(ws, 5, 0)); // APAC / Open

        // Asking about column 1: only the column-0 filter should be honored.
        // Rows where Region=EMEA are kept regardless of Status.
        Assert.False(CellMenu.IsRowHiddenByOtherColumnFilter(ws, 1, 1)); // EMEA / Open
        Assert.False(CellMenu.IsRowHiddenByOtherColumnFilter(ws, 2, 1)); // EMEA / Closed
        Assert.True(CellMenu.IsRowHiddenByOtherColumnFilter(ws, 3, 1));  // AMER / Open
        Assert.True(CellMenu.IsRowHiddenByOtherColumnFilter(ws, 4, 1));  // AMER / Closed
        Assert.True(CellMenu.IsRowHiddenByOtherColumnFilter(ws, 5, 1));  // APAC / Open
    }
}
