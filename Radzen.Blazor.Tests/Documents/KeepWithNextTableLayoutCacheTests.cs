#nullable enable
using Xunit;

using Radzen.Documents;
using Radzen.Documents.Layout;
namespace Radzen.Blazor.Documents.Tests;

using Radzen.Blazor.Pdf.Tests;

public class KeepWithNextTableLayoutCacheTests
{
    [Fact]
    public void KeepWithNextBeforeTable_LaysOutTheTableOnce()
    {
        var fonts = PaginationSupport.Fonts();
        var lh = PaginationSupport.LineHeight(fonts);

        var section = PaginationSupport.Section(400, PaginationSupport.HeightForLines(lh, 6));
        for (var i = 0; i < 4; i++)
        {
            section.Blocks.Add(PaginationSupport.Text($"f{i}"));
        }

        var heading = PaginationSupport.Text("Heading");
        heading.KeepWithNext = true;
        section.Blocks.Add(heading);

        var table = section.Blocks.AddTable();
        table.Columns.Add(Unit.FromPoint(200));
        var row = table.Rows.Add();
        row.Cells[0].Blocks.Add(new Image([1, 2, 3]));

        var measures = 0;
        (double, double) Measure(Image image, double available)
        {
            measures++;
            return (60, 40);
        }

        _ = Paginator.PaginateIsolated(section, fonts, Measure);

        Assert.Equal(1, measures);
    }
}
