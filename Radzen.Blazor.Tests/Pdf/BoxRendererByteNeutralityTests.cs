#nullable enable
using Radzen.Documents.Pdf;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// Layer 1 guard: BoxRenderer.Paint must be a byte-neutral extraction of the cell
// background + border painting. This decoration-heavy document exercises every box
// path: per-cell backgrounds, uniform and non-uniform borders, the cell/row/table
// cascade, rounded cells (uniform -> one rounded stroke; non-uniform -> rounded
// fill + square edges), a whole-table rounded frame, a rounded translucent
// container (ExtGState) and a nested rounded table. The generator emits no /ID or
// dates, so two independent builds must produce identical bytes.
public class BoxRendererByteNeutralityTests
{
    private static void Fill(Cell cell, string text)
    {
        var paragraph = cell.Blocks.AddParagraph();
        var run = paragraph.Inlines.Add(text);
        run.Font.Name = BuildTestSupport.Latin;
        run.Font.Size = 10;
    }

    private static DocumentBuilder BuildDecoratedDocument()
    {
        var builder = new DocumentBuilder();
        BuildTestSupport.RegisterLatin(builder);
        var section = builder.Sections.Add();

        var panel = section.Blocks.Add(new Container
        {
            Padding = Unit.FromPoint(8),
            Background = Color.FromRgb(230, 240, 250),
            CornerRadius = Unit.FromPoint(6),
            Opacity = 0.5,
        });
        panel.Borders.Width = 1;
        panel.Borders.Color = Color.FromRgb(0, 0, 128);
        var text = panel.Blocks.AddParagraph().Inlines.Add("Rounded translucent panel");
        text.Font.Name = BuildTestSupport.Latin;
        text.Font.Size = 10;

        var table = section.Blocks.AddTable();
        table.Borders.Width = 0.75;
        table.Borders.Color = Color.FromRgb(120, 120, 120);
        table.CornerRadius = Unit.FromPoint(4);
        table.Columns.Add(Unit.FromPoint(140));
        table.Columns.Add(Unit.FromPoint(140));

        var first = table.Rows.Add();
        first.Background = Color.FromRgb(245, 245, 245);
        first.Borders.Bottom.Width = 1.5;
        first.Borders.Bottom.Color = Color.FromRgb(200, 0, 0);

        var uniformRounded = first.Cells[0];
        uniformRounded.Background = Color.FromRgb(255, 235, 205);
        uniformRounded.CornerRadius = Unit.FromPoint(5);
        uniformRounded.Borders.Width = 1;
        uniformRounded.Borders.Color = Color.FromRgb(0, 100, 0);
        Fill(uniformRounded, "uniform rounded");

        var mixedRounded = first.Cells[1];
        mixedRounded.Background = Color.FromRgb(220, 255, 220);
        mixedRounded.CornerRadius = Unit.FromPoint(5);
        mixedRounded.Borders.Top.Width = 2;
        mixedRounded.Borders.Left.Width = 0.5;
        mixedRounded.Borders.Left.Style = BorderStyle.Dashed;
        Fill(mixedRounded, "non-uniform rounded");

        var second = table.Rows.Add();
        second.Cells[0].Background = Color.FromRgb(255, 250, 240);
        Fill(second.Cells[0], "row/table cascade");
        var dotted = second.Cells[1];
        dotted.Borders.Width = 1;
        dotted.Borders.Style = BorderStyle.Dotted;
        Fill(dotted, "dotted square");

        var host = section.Blocks.AddTable();
        host.Columns.Add(Unit.FromPoint(280));
        var hostCell = host.Rows.Add().Cells[0];
        hostCell.Background = Color.FromRgb(250, 250, 210);
        hostCell.Borders.Width = 1;

        var nested = hostCell.Blocks.AddTable();
        nested.CornerRadius = Unit.FromPoint(3);
        nested.Borders.Width = 0.5;
        nested.Borders.Color = Color.FromRgb(60, 60, 60);
        nested.Columns.Add(Unit.FromPoint(120));
        var nestedCell = nested.Rows.Add().Cells[0];
        nestedCell.Background = Color.FromRgb(224, 255, 255);
        Fill(nestedCell, "nested");

        return builder;
    }

    [Fact]
    public void DecorationHeavyDocument_BuildsByteIdenticalTwice()
    {
        var golden = BuildDecoratedDocument().ToArray();
        var again = BuildDecoratedDocument().ToArray();

        Assert.Equal(golden, again);
    }
}
