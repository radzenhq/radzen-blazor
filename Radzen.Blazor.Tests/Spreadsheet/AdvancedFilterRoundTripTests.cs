using System.IO;
using System.Linq;
using Radzen.Documents.Spreadsheet;
using Xunit;

namespace Radzen.Blazor.Spreadsheet.Tests;

#nullable enable

// XLSX round-trip for the new filter criterion subclasses (Pass 2C).
// Each test builds a workbook, applies a filter via the per-column API,
// saves it to a stream, reloads, and asserts the criterion comes back.
public class AdvancedFilterRoundTripTests
{
    private static (Workbook wb, Worksheet ws) Build()
    {
        var wb = new Workbook();
        var ws = wb.AddSheet("Sheet1", 11, 3);
        ws.Cells[0, 0].SetValue("Name");
        ws.Cells[0, 1].SetValue("Region");
        ws.Cells[0, 2].SetValue("Amount");
        for (var r = 1; r < 11; r++)
        {
            ws.Cells[r, 0].SetValue($"Row{r}");
            ws.Cells[r, 1].SetValue(r % 2 == 0 ? "EMEA" : "AMER");
            ws.Cells[r, 2].Value = (double)(r * 10);
        }
        ws.AutoFilter.Range = new RangeRef(new CellRef(0, 0), new CellRef(10, 2));
        return (wb, ws);
    }

    private static Worksheet Roundtrip(Workbook wb)
    {
        using var ms = new MemoryStream();
        wb.SaveToStream(ms);
        ms.Position = 0;
        return Workbook.LoadFromStream(ms).Sheets[0];
    }

    [Fact]
    public void RoundTrip_TopFilter_TopThreeItems()
    {
        var (wb, ws) = Build();
        ws.AutoFilter.ApplyTopFilter(2, count: 3, percent: false, bottom: false);

        var loaded = Roundtrip(wb);
        var filter = loaded.Filters.SingleOrDefault();
        Assert.NotNull(filter);
        var top = Assert.IsType<TopFilterCriterion>(filter!.Criterion);
        Assert.Equal(3, top.Count);
        Assert.False(top.Percent);
        Assert.False(top.Bottom);
    }

    [Fact]
    public void RoundTrip_TopFilter_BottomPercent()
    {
        var (wb, ws) = Build();
        ws.AutoFilter.ApplyTopFilter(2, count: 25, percent: true, bottom: true);

        var loaded = Roundtrip(wb);
        var filter = loaded.Filters.Single();
        var top = Assert.IsType<TopFilterCriterion>(filter.Criterion);
        Assert.Equal(25, top.Count);
        Assert.True(top.Percent);
        Assert.True(top.Bottom);
    }

    [Theory]
    [InlineData(DynamicFilterType.AboveAverage)]
    [InlineData(DynamicFilterType.BelowAverage)]
    [InlineData(DynamicFilterType.Today)]
    [InlineData(DynamicFilterType.ThisMonth)]
    [InlineData(DynamicFilterType.LastQuarter)]
    [InlineData(DynamicFilterType.YearToDate)]
    [InlineData(DynamicFilterType.January)]
    [InlineData(DynamicFilterType.Quarter3)]
    public void RoundTrip_DynamicFilter_PreservesType(DynamicFilterType type)
    {
        var (wb, ws) = Build();
        ws.AutoFilter.ApplyDynamicFilter(2, type);

        var loaded = Roundtrip(wb);
        var filter = loaded.Filters.Single();
        var dyn = Assert.IsType<DynamicFilterCriterion>(filter.Criterion);
        Assert.Equal(type, dyn.Type);
    }

    [Fact]
    public void RoundTrip_ColorFilter_PreservesColorAndKind()
    {
        var (wb, ws) = Build();
        // Tag at least one cell with the color so the filter has something to match.
        ws.Cells[2, 0].Format = new Format { BackgroundColor = "#FFFF00" };
        ws.AutoFilter.ApplyColorFilter(0, "#FFFF00", fontColor: false);

        var loaded = Roundtrip(wb);
        var filter = loaded.Filters.Single();
        var color = Assert.IsType<CellColorFilterCriterion>(filter.Criterion);
        Assert.Equal("#FFFF00", color.Color, ignoreCase: true);
        Assert.False(color.FontColor);
    }

    [Fact]
    public void RoundTrip_ColorFilter_FontColor()
    {
        var (wb, ws) = Build();
        ws.Cells[2, 0].Format = new Format { Color = "#FF0000" };
        ws.AutoFilter.ApplyColorFilter(0, "#FF0000", fontColor: true);

        var loaded = Roundtrip(wb);
        var filter = loaded.Filters.Single();
        var color = Assert.IsType<CellColorFilterCriterion>(filter.Criterion);
        Assert.Equal("#FF0000", color.Color, ignoreCase: true);
        Assert.True(color.FontColor);
    }

    [Fact]
    public void RoundTrip_MultipleFilterTypesOnDifferentColumns()
    {
        var (wb, ws) = Build();
        ws.AutoFilter.ApplyValueFilter(1, ["EMEA"]);
        ws.AutoFilter.ApplyTopFilter(2, count: 3);

        var loaded = Roundtrip(wb);
        Assert.Equal(2, loaded.Filters.Count);
        Assert.Contains(loaded.Filters, f => f.Criterion is InListCriterion);
        Assert.Contains(loaded.Filters, f => f.Criterion is TopFilterCriterion);
    }
}
