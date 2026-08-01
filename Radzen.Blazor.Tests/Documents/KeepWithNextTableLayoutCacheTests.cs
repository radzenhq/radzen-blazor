#nullable enable
using System.Linq;
using Xunit;

using Radzen.Documents;
using Radzen.Documents.Layout;
using Radzen.Documents.Core;
namespace Radzen.Blazor.Documents.Tests;

using Radzen.Blazor.Pdf.Tests;
using Radzen.Blazor.Tests.Isolated;

public class KeepWithNextTableLayoutCacheTests
{
    private static Table TableWithImageCell()
    {
        var table = new Table();
        table.Columns.Add(Unit.FromPoint(200));
        var row = table.Rows.Add();
        row.Cells[0].Blocks.Add(new Image(PdfTestResources.ReadAllBytes("Images/rgb.png")));
        return table;
    }

    [Fact]
    public void KeepWithNextBeforeTable_PlacesTheTableOnce()
    {
        var fonts = PaginationSupport.Fonts();
        var lh = PaginationSupport.LineHeight();

        var section = PaginationSupport.Section(400, PaginationSupport.HeightForLines(lh, 6));
        for (var i = 0; i < 4; i++)
        {
            section.Blocks.Add(PaginationSupport.Text($"f{i}"));
        }

        var heading = PaginationSupport.Text("Heading");
        heading.KeepWithNext = true;
        section.Blocks.Add(heading);
        section.Blocks.Add(TableWithImageCell());

        var pages = IsolatedPaginator.PaginateIsolated(section, fonts);

        Assert.Equal(1, pages.Sum(page => page.Body.Tables.Length));
    }

    [Fact]
    public void BlockLayoutCache_ReusesTheLayoutOfABlock()
    {
        var table = TableWithImageCell();
        var cache = new BlockLayoutCache(
            1,
            400,
            PaginationSupport.Fonts(),
            LoweringResult.CreateForDocument(StyleResolution.Empty),
            new LayoutCaptureContext(ImageProbes.None));

        Assert.Same(cache.Table(0, table), cache.Table(0, table));
    }
}
