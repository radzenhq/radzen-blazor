#nullable enable
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Render;
using Radzen.Documents;
using Radzen.Documents.Fonts;
using Radzen.Documents.Layout;
using Radzen.Documents.Geometry;

namespace Radzen.Blazor.Pdf.Tests;

internal static class TableLayoutSupport
{
    public const string Family = "Liberation Sans";

    public static FontCollection Fonts()
    {
        var fonts = new FontCollection();
        fonts.Register(Family, new MemoryStream(
            PdfTestResources.ReadAllBytes("Fonts/LiberationSans-Regular.ttf")));
        return fonts;
    }

    public static Font FontAt(double size) => new() { Family = Family, Size = size };

    public static double Measure(FontCollection fonts, string text, double size)
        => fonts.MeasureText(text, FontAt(size));

    public static double LineHeight(FontCollection fonts, double size = 12)
    {
        var p = new Paragraph();
        var run = p.Inlines.Add("Xg");
        run.Font.Family = Family;
        run.Font.Size = size;
        return LineBreaker.Break(p, 100000, fonts)[0].Height;
    }

    public static Cell Fill(Cell cell, string text, double size = 12)
    {
        cell.Blocks.Clear();
        var p = cell.Blocks.AddParagraph();
        var run = p.Inlines.Add(text);
        run.Font.Family = Family;
        run.Font.Size = size;
        return cell;
    }

    public static LaidOutCell CellAt(LaidOutTable table, int row, int column)
        => table.Cells.Single(c => c.Row == row && c.Column == column);
}
