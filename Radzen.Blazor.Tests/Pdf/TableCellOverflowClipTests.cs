#nullable enable
using System.Collections.Generic;
using System.Linq;
using Radzen.Documents.Pdf;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// An oversized image/QR/barcode in a table cell must be clipped to the cell box so it
// cannot overpaint the neighbouring cell, matching how overflowing cell text is clipped.
public class TableCellOverflowClipTests
{
    private static List<ContentOperation> Ops(DocumentBuilder builder)
        => ContentStreamTokenizer.Parse(ContentTestHelpers.PageContent(BuildTestSupport.Read(builder), 0));

    // Clip rectangles emitted as "x y w h re W n".
    private static List<(double X, double W)> ClipRects(List<ContentOperation> ops)
    {
        var rects = new List<(double, double)>();
        for (var i = 0; i + 2 < ops.Count; i++)
        {
            if (ops[i].Operator == "re" && ops[i + 1].Operator == "W" && ops[i + 2].Operator == "n")
            {
                rects.Add((ops[i].Num(0), ops[i].Num(2)));
            }
        }

        return rects;
    }

    private static List<(double X, double W)> FillRects(List<ContentOperation> ops)
    {
        var rects = new List<(double, double)>();
        for (var i = 0; i + 1 < ops.Count; i++)
        {
            if (ops[i].Operator == "re" && ops[i + 1].Operator == "f")
            {
                rects.Add((ops[i].Num(0), ops[i].Num(2)));
            }
        }

        return rects;
    }

    [Fact]
    public void OversizedQrCode_InNarrowCell_IsClippedToCellBox()
    {
        var builder = new DocumentBuilder();
        var section = builder.Sections.Add();
        section.PageSize = new PageSize(Unit.FromPoint(400), Unit.FromPoint(500));
        section.Margin = Unit.FromPoint(20);
        var table = section.Blocks.AddTable();
        table.Columns.Add().Width = Unit.FromPoint(40);
        table.Columns.Add().Width = Unit.FromPoint(200);
        var row = table.Rows.Add();
        row.Cells[0].Blocks.AddQrCode("overflow", Unit.FromPoint(90));
        row.Cells[1].Blocks.AddParagraph("neighbour");

        var ops = Ops(builder);
        var moduleRight = FillRects(ops).Max(r => r.X + r.W);

        var clips = ClipRects(ops);
        Assert.NotEmpty(clips);

        // The QR modules really extend past the narrow cell, and the clip's right edge
        // sits well left of the rightmost module, so the module fills cannot paint into
        // the neighbouring cell.
        var clipRight = clips.Min(c => c.X + c.W);
        Assert.True(moduleRight > clipRight + 1,
            $"QR modules (right {moduleRight}) should overflow the clip box (right {clipRight})");
    }
}
