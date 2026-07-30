#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

public class FieldOverflowGuardCoverageTests
{
    private static List<(string Text, double Y)> PlacedText(DocumentReader reader, int page)
    {
        var content = Encoding.Latin1.GetString(ContentTestHelpers.PageContent(reader, page));
        var result = new List<(string, double)>();
        foreach (Match m in Regex.Matches(
            content,
            @"(-?[\d.]+)\s+(-?[\d.]+)\s+Td\s*\((.*?)\)\s*Tj",
            RegexOptions.Singleline))
        {
            result.Add((m.Groups[3].Value, double.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture)));
        }

        return result;
    }

    private static double PlaceholderWidth()
    {
        var fonts = LineLayoutSupport.Fonts();
        return fonts.MeasureText("ending on page 0", LineLayoutSupport.FontAt(12));
    }

    private static Paragraph FieldParagraph()
    {
        var paragraph = new Paragraph();
        var text = paragraph.Inlines.Add("ending on page ");
        text.Font.Family = LineLayoutSupport.Family;
        text.Font.Size = 12;
        var field = paragraph.Inlines.Add(new PageCountField());
        field.Font.Family = LineLayoutSupport.Family;
        field.Font.Size = 12;
        return paragraph;
    }

    private static Paragraph Plain(string text)
    {
        var paragraph = new Paragraph();
        var run = paragraph.Inlines.Add(text);
        run.Font.Family = LineLayoutSupport.Family;
        run.Font.Size = 12;
        return paragraph;
    }

    private static Document Author(out Section section)
    {
        var document = new Document();
        BuildTestSupport.RegisterLatin(document);
        section = document.Sections.Add();
        section.PageSize = new PageSize(Unit.FromPoint(PlaceholderWidth() + 1 + 80), Unit.FromPoint(500));
        section.Margins.SetAll(Unit.FromPoint(40));
        return document;
    }

    private static void PadToTenPages(Section section)
    {
        for (var i = 0; i < 10; i++)
        {
            section.Blocks.AddPageBreak();
        }
    }

    [Fact]
    public void BodyFieldParagraph_ResolvedValueWrapsBeyondLaidOutLines_Throws()
    {
        var document = Author(out var section);
        section.Blocks.Add(FieldParagraph());
        section.Blocks.Add(Plain("BELOW"));
        PadToTenPages(section);

        var ex = Record.Exception(() => new DocumentRenderer().ToArray(document));

        Assert.NotNull(ex);
        Assert.IsType<InvalidOperationException>(ex);
        Assert.Contains("reserved", ex!.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TableCellFieldParagraph_ResolvedValueWrapsBeyondLaidOutLines_Throws()
    {
        var document = Author(out var section);
        var table = section.Blocks.AddTable();
        table.Columns.Add();
        var row = table.Rows.Add();
        var cell = row.Cells[0];
        cell.Blocks.Add(FieldParagraph());
        cell.Blocks.Add(Plain("BELOW"));
        PadToTenPages(section);

        var ex = Record.Exception(() => new DocumentRenderer().ToArray(document));

        Assert.NotNull(ex);
        Assert.IsType<InvalidOperationException>(ex);
        Assert.Contains("reserved", ex!.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BodyFieldParagraph_ResolvedValueFitsReservedLines_RendersWithoutOverprinting()
    {
        var document = Author(out var section);
        section.Blocks.Add(FieldParagraph());
        section.Blocks.Add(Plain("BELOW"));

        var reader = BuildTestSupport.Read(document);
        var placed = PlacedText(reader, 0);

        Assert.Equal(2, placed.Count);
        Assert.NotEqual(placed[0].Y, placed[1].Y);
    }
}
