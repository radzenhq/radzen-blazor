#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Radzen.Documents.Pdf;
using Xunit;
using Radzen.Documents;
using Radzen.Documents.Fonts;

namespace Radzen.Blazor.Pdf.Tests;

public class MeasureEmitRegressionTests
{
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

    private static List<(double X, double Y, byte[] Bytes)> ShowsSortedTopDown(
        Document document,
        DocumentRenderer? renderer = null)
    {
        var reader = BuildTestSupport.Read(document, renderer);
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
            ((Run)paragraph.Inlines[0]).Font.Family = family;
        }

        ((Run)paragraph.Inlines[0]).Font.Size = 12;
        return paragraph;
    }

    [Fact]
    public void Base14NoFallback_RightAligned_SubstituteDoesNotRewriteLayoutMetrics()
    {
        var document = new Document();
        var section = document.Sections.Add();
        RightAligned(section, "ﬁﬁﬁﬁﬁ", null);
        RightAligned(section, "?????", null);

        var shows = ShowsSortedTopDown(
            document,
            new DocumentRenderer { AllowUnsupportedCharacters = true });

        Assert.Equal(2, shows.Count);
        Assert.Equal("?????", Encoding.Latin1.GetString(shows[0].Bytes));
        Assert.Equal("?????", Encoding.Latin1.GetString(shows[1].Bytes));
        var font = new Font { Size = 12 };
        var expected = document.Fonts.MeasureText("?????", font) - document.Fonts.MeasureText("ﬁﬁﬁﬁﬁ", font);
        Assert.Equal(expected, shows[0].X - shows[1].X, 2);
    }

    [Fact]
    public void Base14WithFallback_RightAligned_FallbackWidthsDrivePosition()
    {
        var document = new Document();
        BuildTestSupport.RegisterLatin(document);
        document.Fonts.SetFallback(BuildTestSupport.Latin);

        var section = document.Sections.Add();
        RightAligned(section, "лева", null);
        RightAligned(section, "лева", BuildTestSupport.Latin);

        var shows = ShowsSortedTopDown(document);

        Assert.Equal(2, shows.Count);
        Assert.Equal(shows[1].Bytes, shows[0].Bytes);
        Assert.True(Math.Abs(shows[0].X - shows[1].X) < 0.2,
            $"fallback-served base-14 line at x={shows[0].X} but native line at x={shows[1].X}");
    }

    [Fact]
    public void SfntPrimary_SurrogatePair_RightAligned_MeasuredOnce()
    {
        var document = new Document();
        BuildTestSupport.RegisterLatin(document);

        var section = document.Sections.Add();
        RightAligned(section, "A\U0001F600B", BuildTestSupport.Latin);
        RightAligned(section, "A中B", BuildTestSupport.Latin);

        var shows = ShowsSortedTopDown(document);

        Assert.Equal(2, shows.Count);
        Assert.Equal(shows[1].Bytes, shows[0].Bytes);
        Assert.True(Math.Abs(shows[0].X - shows[1].X) < 0.2,
            $"surrogate-pair line at x={shows[0].X} but BMP-notdef line at x={shows[1].X}");
    }

    [Fact]
    public void SimpleShaper_SurrogatePair_YieldsSingleGlyph()
    {
        var fonts = LineLayoutSupport.Fonts();
        var shaper = new SimpleShaper(fonts);
        var font = LineLayoutSupport.FontAt(12);

        var glyphs = shaper.Shape("A\U0001F600", font, out var advance);

        Assert.Equal(2, glyphs.Count);
        Assert.Equal(fonts.MeasureText("A\U0001F600", font), advance, 6);
    }

    [Fact]
    public void MeasureText_SurrogatePair_CountsOneCodepoint()
    {
        var fonts = LineLayoutSupport.Fonts();
        var font = LineLayoutSupport.FontAt(12);

        Assert.Equal(fonts.MeasureText("A中B", font), fonts.MeasureText("A\U0001F600B", font), 6);
    }

    [Fact]
    public void SfntEmission_DrawsExactlyTheShaperGlyphSequence()
    {
        const string word = "Typographic";

        var document = new Document();
        BuildTestSupport.RegisterLatin(document);
        var section = document.Sections.Add();
        BuildTestSupport.AddText(section, word, BuildTestSupport.Latin);

        var shows = ShowsSortedTopDown(document);
        Assert.Single(shows);
        var drawn = Cids(shows[0].Bytes);

        var shaper = new SimpleShaper(LineLayoutSupport.Fonts());
        var glyphs = shaper.Shape(word, LineLayoutSupport.FontAt(12), out _);
        var shaped = glyphs.Select(glyph => glyph.GlyphId).ToArray();

        Assert.Equal(shaped.Length, drawn.Length);
        Assert.Equal(shaped[2], shaped[7]);
        for (var a = 0; a < shaped.Length; a++)
        {
            for (var b = 0; b < shaped.Length; b++)
            {
                Assert.Equal(shaped[a] == shaped[b], drawn[a] == drawn[b]);
            }
        }
    }

    private static ushort[] Cids(byte[] bytes)
    {
        var cids = new ushort[bytes.Length / 2];
        for (var i = 0; i < cids.Length; i++)
        {
            cids[i] = (ushort)((bytes[(2 * i)] << 8) | bytes[(2 * i) + 1]);
        }

        return cids;
    }
}
