#nullable enable

using System;
using System.IO;
using System.Text;
using Radzen.Documents.Pdf;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// A simple font's /Widths need only cover FirstChar..LastChar; every other code takes the
// descriptor's /MissingWidth (ISO 32000-1 9.6.2.1), which the reader does not read, so those
// codes reach TextSearch with no usable width and are measured at the average-glyph estimate.
//
// The fixture pins the divergence that estimate creates. Its font gives a width for X only,
// and /MissingWidth 1000 makes each W advance a full em: at size 10 from x=10 the four Ws
// really end at x=50 and the X really occupies 50..56.67, while the 0.5em estimate puts the
// whole run inside 10..36.67 - short of, and disjoint from, where the X really is.
public class EstimatedTextGeometryTests
{
    private static readonly PdfRect TrueXBounds = new(50, 700, 56.67, 710);

    private static Document LoadedDocument(bool widthForW)
    {
        const string stream = "BT /F1 10 Tf 10 700 Td (WWWWX) Tj ET";
        var widths = widthForW ? "/FirstChar 87 /LastChar 88 /Widths [1000 667]" : "/FirstChar 88 /LastChar 88 /Widths [667]";
        var pdf = new FixturePdf()
            .Append("%PDF-1.7\n")
            .Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n")
            .Object(2, "2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n")
            .Object(3, "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] "
                + "/Resources << /Font << /F1 5 0 R >> >> /Contents 4 0 R >>\nendobj\n")
            .Object(4, $"4 0 obj\n<< /Length {stream.Length} >>\nstream\n{stream}\nendstream\nendobj\n")
            .Object(5, "5 0 obj\n<< /Type /Font /Subtype /TrueType /BaseFont /Widthless /Encoding /WinAnsiEncoding "
                + widths + " /FontDescriptor 6 0 R >>\nendobj\n")
            .Object(6, "6 0 obj\n<< /Type /FontDescriptor /FontName /Widthless /Flags 32 /ItalicAngle 0 "
                + "/Ascent 700 /Descent -200 /CapHeight 700 /StemV 80 /FontBBox [0 -200 1000 900] /MissingWidth 1000 >>\nendobj\n");
        var xref = pdf.Position;
        pdf.Append("xref\n0 7\n").Append(FixturePdf.Entry20(0, 65535, 'f'));
        for (var number = 1; number <= 6; number++)
        {
            pdf.Append(FixturePdf.Entry20(pdf.OffsetOf(number)));
        }

        pdf.Append("trailer\n<< /Size 7 /Root 1 0 R >>\nstartxref\n" + xref + "\n%%EOF\n");
        using var input = new MemoryStream(pdf.ToArray());
        return Document.LoadFromStream(input);
    }

    private static string SavedContent(Document document)
        => Encoding.Latin1.GetString(InterpreterTestSupport.PageContentBytes(document.ToArray(), 0));

    // The control: spelling out the same width the descriptor implies leaves nothing estimated,
    // and the search then agrees with where the glyph really is.
    [Fact]
    public void FindText_WithEveryWidth_IsExactAndNotEstimated()
    {
        var hit = Assert.Single(LoadedDocument(widthForW: true).Pages[0].FindText("X"));

        Assert.False(hit.GeometryEstimated);
        Assert.Equal(TrueXBounds.Left, hit.Bounds.Left, 2);
        Assert.Equal(TrueXBounds.Right, hit.Bounds.Right, 2);
    }

    // Search still answers on a partial-width font, but says its geometry is an estimate - and
    // the estimate here is not merely imprecise, it does not overlap the glyph at all.
    [Fact]
    public void FindText_WithMissingWidth_AnswersButReportsEstimatedGeometry()
    {
        var hit = Assert.Single(LoadedDocument(widthForW: false).Pages[0].FindText("X"));

        Assert.True(hit.GeometryEstimated);
        Assert.True(hit.Bounds.Right < TrueXBounds.Left);
    }

    [Fact]
    public void ExtractPositionedText_ReportsWhichRunsAreEstimated()
    {
        Assert.False(Assert.Single(LoadedDocument(widthForW: true).Pages[0].ExtractPositionedText()).GeometryEstimated);
        Assert.True(Assert.Single(LoadedDocument(widthForW: false).Pages[0].ExtractPositionedText()).GeometryEstimated);
    }

    [Fact]
    public void ReplaceText_WithMissingWidth_Throws()
        => Assert.Throws<NotSupportedException>(() => LoadedDocument(widthForW: false).Pages[0].ReplaceText("W", "X"));

    // The failure this pins: the caller names the area the X really occupies, the estimate puts
    // every glyph short of it, so nothing is selected for removal and the fill is painted over
    // text that is still there. Redaction must refuse rather than report a cover it did not make.
    [Fact]
    public void Redact_AreaOverAnEstimatedRun_Throws()
    {
        var document = LoadedDocument(widthForW: false);

        Assert.Throws<NotSupportedException>(
            () => document.Pages[0].Redact([TrueXBounds], new RedactionOptions { FillColor = Color.Black }));
    }

    [Fact]
    public void Redact_AreaOverAnExactRun_RemovesTheGlyph()
    {
        var document = LoadedDocument(widthForW: true);

        document.Pages[0].Redact([TrueXBounds], new RedactionOptions { FillColor = Color.Black });

        Assert.DoesNotContain("(WWWWX)", SavedContent(document), StringComparison.Ordinal);
        Assert.DoesNotContain("X", document.Pages[0].ExtractText(), StringComparison.Ordinal);
    }

    [Fact]
    public void RedactText_MatchingAnEstimatedRun_Throws()
        => Assert.Throws<NotSupportedException>(() => LoadedDocument(widthForW: false).Pages[0].RedactText("X"));

    // No width makes a run paint off its own baseline, so an estimate must not veto a redaction
    // it provably cannot reach.
    [Fact]
    public void Redact_AreaOffTheEstimatedRunsBaseline_Succeeds()
    {
        var document = LoadedDocument(widthForW: false);

        document.Pages[0].Redact([PdfRect.FromSize(10, 10, 50, 50)]);

        Assert.Contains("(WWWWX)", SavedContent(document), StringComparison.Ordinal);
    }

    // Uncertainty runs forward along the baseline only: the origin is exact, so an area behind
    // the run's start is out of its reach however wide the real glyphs turn out to be.
    [Fact]
    public void Redact_AreaBehindTheEstimatedRunsOrigin_Succeeds()
    {
        var document = LoadedDocument(widthForW: false);

        document.Pages[0].Redact([new PdfRect(0, 700, 9, 710)]);

        Assert.Contains("(WWWWX)", SavedContent(document), StringComparison.Ordinal);
    }
}
