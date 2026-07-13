#nullable enable
using System;
using System.Collections.Generic;
using System.Text;
using Radzen.Documents.Pdf;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// R4(a) MEASURE == EMIT: the widths used for line breaking and alignment must be
// the widths of the glyphs that are actually drawn. Base-14 measurement must not
// give zero width to non-WinAnsi characters that emission renders (via the
// registered fallback chain or a '?' substitute), and text must be resolved by
// Unicode codepoints (not UTF-16 code units) in measurement exactly as in emission.
// Each test compares the emitted Td x of two right-aligned lines whose DRAWN glyphs
// are identical - so their measured widths, and therefore their positions, must match.
public class MeasureEmitRegressionTests
{
    // Collects (x, y, operand) triples: the Td position active at each Tj.
    private static List<(double X, double Y, byte[] Bytes)> TextShows(byte[] content)
    {
        var shows = new List<(double, double, byte[])>();
        double x = 0, y = 0;
        foreach (var op in ContentStreamTokenizer.Parse(content))
        {
            if (op.Operator == "Td")
            {
                x = op.Num(0);
                y = op.Num(1);
            }
            else if (op.Operator == "Tj" && op.Operands.Count >= 1)
            {
                shows.Add((x, y, op.Operands[0].Bytes));
            }
        }

        return shows;
    }

    private static List<(double X, double Y, byte[] Bytes)> ShowsSortedTopDown(DocumentBuilder builder)
    {
        var reader = BuildTestSupport.Read(builder);
        var shows = TextShows(ContentTestHelpers.PageContent(reader, 0));
        shows.Sort((a, b) => b.Y.CompareTo(a.Y));
        return shows;
    }

    private static Paragraph RightAligned(Section section, string text, string? family)
    {
        var paragraph = section.Blocks.AddParagraph(text);
        paragraph.Alignment = HorizontalAlignment.Right;
        if (family is not null)
        {
            paragraph.Inlines[0].Font.Name = family;
        }

        paragraph.Inlines[0].Font.Size = 12;
        return paragraph;
    }

    // No fallback registered: emission substitutes '?' for each non-cp1252
    // character, so a right-aligned Cyrillic line must sit at the same x as a
    // right-aligned line of the same number of literal question marks.
    [Fact]
    public void Base14NoFallback_RightAligned_SubstituteDrivesPosition()
    {
        var builder = new DocumentBuilder();
        var section = builder.Sections.Add();
        RightAligned(section, "ЙЙЙЙЙ", null);
        RightAligned(section, "?????", null);

        var shows = ShowsSortedTopDown(builder);

        Assert.Equal(2, shows.Count);
        Assert.Equal("?????", Encoding.Latin1.GetString(shows[0].Bytes));
        Assert.Equal("?????", Encoding.Latin1.GetString(shows[1].Bytes));
        Assert.True(Math.Abs(shows[0].X - shows[1].X) < 0.2,
            $"right-aligned substitute line at x={shows[0].X} but literal '?????' line at x={shows[1].X}");
    }

    // With a fallback registered, non-WinAnsi characters draw through the fallback
    // face with its real advances. A base-14 paragraph whose glyphs all come from
    // the fallback must be right-aligned to the same x as the same text set
    // directly in that face - both lines draw the identical glyphs.
    [Fact]
    public void Base14WithFallback_RightAligned_FallbackWidthsDrivePosition()
    {
        var builder = new DocumentBuilder();
        BuildTestSupport.RegisterLatin(builder);
        builder.Fonts.SetFallback(BuildTestSupport.Latin);

        var section = builder.Sections.Add();
        RightAligned(section, "лева", null); // base-14 primary, all glyphs from fallback
        RightAligned(section, "лева", BuildTestSupport.Latin); // same glyphs, primary face

        var shows = ShowsSortedTopDown(builder);

        Assert.Equal(2, shows.Count);
        Assert.Equal(shows[1].Bytes, shows[0].Bytes); // identical glyph ids drawn
        Assert.True(Math.Abs(shows[0].X - shows[1].X) < 0.2,
            $"fallback-served base-14 line at x={shows[0].X} but native line at x={shows[1].X}");
    }

    // Emission iterates Unicode codepoints, so a supplementary-plane character
    // draws exactly one glyph (.notdef here). Measurement must not count its two
    // UTF-16 code units separately: a right-aligned line containing a surrogate
    // pair must land at the same x as one containing a single unmapped BMP
    // character - both draw the same A/.notdef/B glyph sequence.
    [Fact]
    public void SfntPrimary_SurrogatePair_RightAligned_MeasuredOnce()
    {
        var builder = new DocumentBuilder();
        BuildTestSupport.RegisterLatin(builder);

        var section = builder.Sections.Add();
        RightAligned(section, "A\U0001F600B", BuildTestSupport.Latin);
        RightAligned(section, "A中B", BuildTestSupport.Latin);

        var shows = ShowsSortedTopDown(builder);

        Assert.Equal(2, shows.Count);
        Assert.Equal(shows[1].Bytes, shows[0].Bytes); // identical glyph ids drawn
        Assert.True(Math.Abs(shows[0].X - shows[1].X) < 0.2,
            $"surrogate-pair line at x={shows[0].X} but BMP-notdef line at x={shows[1].X}");
    }

    // The shaper resolves glyphs per codepoint like emission does: a surrogate
    // pair is a single glyph, and the run's advance equals MeasureText.
    [Fact]
    public void SimpleShaper_SurrogatePair_YieldsSingleGlyph()
    {
        var fonts = LineLayoutSupport.Fonts();
        var shaper = new SimpleShaper(fonts);
        var font = LineLayoutSupport.FontAt(12);

        var run = shaper.Shape("A\U0001F600", font, FlowDirection.LeftToRight, WritingMode.HorizontalTopToBottom);

        Assert.Equal(2, run.Glyphs.Count);
        Assert.Equal(fonts.MeasureText("A\U0001F600", font), run.Advance, 6);
    }

    // MeasureText itself must resolve by codepoint: the surrogate pair is one
    // unmapped codepoint, so it measures like any single unmapped BMP codepoint.
    [Fact]
    public void MeasureText_SurrogatePair_CountsOneCodepoint()
    {
        var fonts = LineLayoutSupport.Fonts();
        var font = LineLayoutSupport.FontAt(12);

        Assert.Equal(fonts.MeasureText("A中B", font), fonts.MeasureText("A\U0001F600B", font), 6);
    }
}
