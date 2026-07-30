#nullable enable
using System;
using System.Linq;
using Radzen.Documents;
using Radzen.Documents.Fonts;
using Radzen.Documents.Layout;
using Radzen.Blazor.Pdf.Tests;
using Xunit;

namespace Radzen.Blazor.Documents.Tests;

public class BuiltInFontCoverageTests
{
    private const string Cjk = "Noto Sans SC";
    private const string CjkFile = "Fonts/NotoSansSC-Subset.otf";

    private static Document WithText(string text)
    {
        var document = new Document();
        document.Sections.Add().Blocks.AddParagraph(text);
        return document;
    }

    [Fact]
    public void MeasureText_WithWinAnsiText_DoesNotThrow()
    {
        var fonts = new FontCollection();

        Assert.True(fonts.MeasureText("Hello, world!", new Font()) > 0);
    }

    [Fact]
    public void MeasureText_WithUncoveredCharacter_ThrowsNamingTheCharacterAndTheFont()
    {
        var fonts = new FontCollection();

        var error = Assert.Throws<InvalidOperationException>(() => fonts.MeasureText("hi 中", new Font()));

        Assert.Contains("U+4E2D", error.Message, StringComparison.Ordinal);
        Assert.Contains("Helvetica", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void MeasureText_WithSeveralUncoveredCharacters_NamesEachOfThemOnce()
    {
        var fonts = new FontCollection();

        var error = Assert.Throws<InvalidOperationException>(() => fonts.MeasureText("中中丁", new Font()));

        Assert.Contains("U+4E2D", error.Message, StringComparison.Ordinal);
        Assert.Contains("U+4E01", error.Message, StringComparison.Ordinal);
        Assert.Equal(1, Occurrences(error.Message, "U+4E2D"));
    }

    [Fact]
    public void MeasureText_WithUncoveredAstralCharacter_NamesItsCodePoint()
    {
        var fonts = new FontCollection();

        var error = Assert.Throws<InvalidOperationException>(() => fonts.MeasureText("\U0001F600", new Font()));

        Assert.Contains("U+1F600", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void MeasureText_AllowUnsupportedCharacters_DoesNotChangeNeutralMetrics()
    {
        var fonts = new FontCollection { AllowUnsupportedCharacters = true };

        Assert.NotEqual(fonts.MeasureText("?", new Font()), fonts.MeasureText("ﬁ", new Font()));
    }

    [Fact]
    public void Layout_WithUncoveredCharacter_Throws()
    {
        var document = WithText("café 中");

        Assert.Throws<InvalidOperationException>(() => DocumentLayouter.Layout(document));
    }

    [Fact]
    public void Layout_WithMeasurableNonWinAnsiCharacter_CapturesUnicode()
    {
        var document = WithText("ﬁ");

        var laidOut = DocumentLayouter.Layout(document);
        var glyph = Assert.Single(
            Assert.Single(laidOut.Pages[0].Body.Lines[0].Line.Fragments[0].GlyphRun.Spans)
                .BuiltInGlyphs);

        Assert.Equal(0xFB01, glyph.Codepoint);
    }

    [Fact]
    public void Layout_WithARegisteredFontCoveringTheCharacter_DoesNotThrow()
    {
        var document = WithText("\u4E2D");
        document.Fonts.Register(Cjk, new System.IO.MemoryStream(PdfTestResources.ReadAllBytes(CjkFile)));
        ((Paragraph)document.Sections[0].Blocks[0]).Font.Family = Cjk;

        Assert.Null(Record.Exception(() => DocumentLayouter.Layout(document)));
    }

    [Fact]
    public void Layout_WithAFallbackFontCoveringTheCharacter_DoesNotThrow()
    {
        var document = WithText("\u4E2D");
        document.Fonts.Register(Cjk, new System.IO.MemoryStream(PdfTestResources.ReadAllBytes(CjkFile)));
        document.Fonts.SetFallback(Cjk);

        Assert.Null(Record.Exception(() => DocumentLayouter.Layout(document)));
    }

    [Fact]
    public void Watermark_WithUncoveredCharacterAndABuiltInFont_Throws()
    {
        var document = new Document();
        var section = document.Sections.Add();
        section.Blocks.AddParagraph("body");
        section.Watermark = new Watermark { Text = "中" };

        Assert.Throws<InvalidOperationException>(() => DocumentLayouter.Layout(document));
    }

    private static int Occurrences(string text, string value)
    {
        var count = 0;
        var index = text.IndexOf(value, StringComparison.Ordinal);
        while (index >= 0)
        {
            count++;
            index = text.IndexOf(value, index + value.Length, StringComparison.Ordinal);
        }

        return count;
    }
}
