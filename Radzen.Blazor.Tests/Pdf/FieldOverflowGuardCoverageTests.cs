#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// #48 follow-up: the overflow guard in FieldResolver is not a band concern. A body
// paragraph and a table cell are paginated/sized from the field's PLACEHOLDER layout
// too, so a resolved value that wraps to more lines than were laid out overprints the
// content below it exactly as it would in a band. Every call site reserves; every call
// site must pass its reservation.
public class FieldOverflowGuardCoverageTests
{
    // (text, baseline) for every show on the page, in emission order.
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
        text.Font.Name = LineLayoutSupport.Family;
        text.Font.Size = 12;
        var field = paragraph.Inlines.Add(new PageCountField());
        field.Font.Name = LineLayoutSupport.Family;
        field.Font.Size = 12;
        return paragraph;
    }

    private static Paragraph Plain(string text)
    {
        var paragraph = new Paragraph();
        var run = paragraph.Inlines.Add(text);
        run.Font.Name = LineLayoutSupport.Family;
        run.Font.Size = 12;
        return paragraph;
    }

    // A section whose content width fits "ending on page 0" on one line but not
    // "ending on page 10", plus enough page breaks to make the resolved count "10".
    private static DocumentBuilder Author(out Section section)
    {
        var builder = new DocumentBuilder();
        BuildTestSupport.RegisterLatin(builder);
        section = builder.Sections.Add();
        section.PageSize = new PageSize(Unit.FromPoint(PlaceholderWidth() + 1 + 80), Unit.FromPoint(500));
        section.Margin = Unit.FromPoint(40);
        return builder;
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
        var builder = Author(out var section);
        section.Blocks.Add(FieldParagraph());
        section.Blocks.Add(Plain("BELOW"));
        PadToTenPages(section);

        var ex = Record.Exception(() => builder.ToArray());

        Assert.NotNull(ex);
        Assert.IsType<InvalidOperationException>(ex);
        Assert.Contains("reserved", ex!.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TableCellFieldParagraph_ResolvedValueWrapsBeyondLaidOutLines_Throws()
    {
        var builder = Author(out var section);
        var table = section.Blocks.AddTable();
        table.Columns.Add();
        var row = table.Rows.Add();
        var cell = row.Cells[0];
        cell.Blocks.Add(FieldParagraph());
        cell.Blocks.Add(Plain("BELOW"));
        PadToTenPages(section);

        var ex = Record.Exception(() => builder.ToArray());

        Assert.NotNull(ex);
        Assert.IsType<InvalidOperationException>(ex);
        Assert.Contains("reserved", ex!.Message, StringComparison.OrdinalIgnoreCase);
    }

    // The guard must not fire when the resolved value still fits: all reserved lines render,
    // each on its own baseline. This is what the two throw tests would otherwise pass by
    // vacuously refusing to emit anything.
    [Fact]
    public void BodyFieldParagraph_ResolvedValueFitsReservedLines_RendersWithoutOverprinting()
    {
        var builder = Author(out var section);
        section.Blocks.Add(FieldParagraph());
        section.Blocks.Add(Plain("BELOW"));

        var reader = BuildTestSupport.Read(builder);
        var placed = PlacedText(reader, 0);

        Assert.Equal(2, placed.Count);
        Assert.NotEqual(placed[0].Y, placed[1].Y);
    }
}
