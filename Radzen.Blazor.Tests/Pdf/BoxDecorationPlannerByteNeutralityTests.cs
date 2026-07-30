#nullable enable
using Radzen.Documents.Pdf;
using Xunit;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

public class BoxDecorationPlannerByteNeutralityTests
{
    private static void Fill(Cell cell, string text)
    {
        var paragraph = cell.Blocks.AddParagraph();
        var run = paragraph.Inlines.Add(text);
        run.Font.Family = BuildTestSupport.Latin;
        run.Font.Size = 10;
    }

    private static Document BuildDecoratedDocument()
    {
        var document = new Document();
        BuildTestSupport.RegisterLatin(document);
        var section = document.Sections.Add();

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
        text.Font.Family = BuildTestSupport.Latin;
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

        var uniformCell = first.Cells[0];
        uniformCell.Background = Color.FromRgb(255, 235, 205);
        uniformCell.Borders.Width = 1;
        uniformCell.Borders.Color = Color.FromRgb(0, 100, 0);
        Fill(uniformCell, "uniform border");

        var mixedCell = first.Cells[1];
        mixedCell.Background = Color.FromRgb(220, 255, 220);
        mixedCell.Borders.Top.Width = 2;
        mixedCell.Borders.Left.Width = 0.5;
        mixedCell.Borders.Left.Style = BorderStyle.Dashed;
        Fill(mixedCell, "non-uniform border");

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

        return document;
    }

    [Fact]
    public void DecorationHeavyDocument_BuildsByteIdenticalTwice()
    {
        var golden = new DocumentRenderer().ToArray(BuildDecoratedDocument());
        var again = new DocumentRenderer().ToArray(BuildDecoratedDocument());

        Assert.Equal(golden, again);
    }
}
