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
using Radzen.Documents.Core;

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

    private static void Sized(Inline inline)
    {
        var run = (TextInline)inline;
        run.Font.Family = LineLayoutSupport.Family;
        run.Font.Size = 12;
    }

    private static Paragraph FieldParagraph()
    {
        var paragraph = new Paragraph();
        var text = paragraph.Inlines.Add("ending on page ");
        text.Font.Family = LineLayoutSupport.Family;
        text.Font.Size = 12;
        var field = new PageCountField();
        paragraph.Inlines.Add(field);
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
            section.Blocks.Add(new PageBreak());
        }
    }

    [Fact]
    public void BodyFieldParagraph_MultiDigitCount_FitsTheReservedLines()
    {
        var document = Author(out var section);
        section.Blocks.Add(FieldParagraph());
        section.Blocks.Add(Plain("BELOW"));
        PadToTenPages(section);

        Assert.Null(Record.Exception(() => new DocumentRenderer().ToArray(document)));
    }

    [Fact]
    public void TableCellFieldParagraph_MultiDigitCount_FitsTheReservedLines()
    {
        var document = Author(out var section);
        var table = section.Blocks.Add(new Table());
        table.Columns.Add();
        var row = table.Rows.Add();
        var cell = row.Cells[0];
        cell.Blocks.Add(FieldParagraph());
        cell.Blocks.Add(Plain("BELOW"));
        PadToTenPages(section);

        Assert.Null(Record.Exception(() => new DocumentRenderer().ToArray(document)));
    }

    [Fact]
    public void HeaderFieldParagraph_OverAHundredPages_FitsTheReservedLines()
    {
        var document = new Document();
        BuildTestSupport.RegisterLatin(document);
        var section = document.Sections.Add();
        var fonts = LineLayoutSupport.Fonts();
        var narrow = fonts.MeasureText("Page 0 of 0", LineLayoutSupport.FontAt(12));
        section.PageSize = new PageSize(Unit.FromPoint(narrow + 1 + 80), Unit.FromPoint(500));
        section.Margins.SetAll(Unit.FromPoint(40));

        var header = new Paragraph();
        Sized(header.Inlines.Add("Page "));
        Sized(header.Inlines.Add(new PageNumberField()));
        Sized(header.Inlines.Add(" of "));
        Sized(header.Inlines.Add(new PageCountField()));
        section.Header.Blocks.Add(header);
        for (var i = 0; i < 120; i++)
        {
            section.Blocks.Add(new PageBreak());
        }

        Assert.Null(Record.Exception(() => new DocumentRenderer().ToArray(document)));
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
