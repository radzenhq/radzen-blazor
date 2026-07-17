#nullable enable

using System.IO;
using System.Linq;
using Radzen.Documents.Pdf;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class ContentSpacingDivergenceTests
{
    private static Document Loaded(string streamData)
    {
        var contentObject = $"4 0 obj\n<< /Length {streamData.Length} >>\nstream\n{streamData}\nendstream\nendobj\n";
        var pdf = new FixturePdf()
            .Append("%PDF-1.7\n")
            .Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n")
            .Object(2, "2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n")
            .Object(3, "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] "
                + "/Resources << /Font << /F0 5 0 R >> >> /Contents 4 0 R >>\nendobj\n")
            .Object(4, contentObject)
            .Object(5, "5 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>\nendobj\n");
        var xref = pdf.Position;
        pdf.Append("xref\n0 6\n").Append(FixturePdf.Entry20(0, 65535, 'f'));
        for (var number = 1; number <= 5; number++)
        {
            pdf.Append(FixturePdf.Entry20(pdf.OffsetOf(number)));
        }

        pdf.Append("trailer\n<< /Size 6 /Root 1 0 R >>\nstartxref\n" + xref + "\n%%EOF\n");
        using var input = new MemoryStream(pdf.ToArray());
        return Document.LoadFromStream(input);
    }

    // The " operator sets word spacing for every following show, so a PreserveAdvance
    // replacement that drops a space must compensate for the word spacing it removed.
    [Fact]
    public void ReplaceText_AfterQuoteShowSetsWordSpacing_PreservesFollowingOrigin()
    {
        const string stream = "BT /F0 10 Tf 20 TL 1 0 0 1 72 700 Tm 15 0 (X) \" (A B) Tj (Z) Tj ET";
        var loaded = Loaded(stream);
        var before = loaded.Pages[0].ExtractPositionedText().Single(run => run.Text == "Z").Bounds.Left;

        var count = loaded.ReplaceText("A B", "AB");
        var reloaded = InterpreterTestSupport.Load(loaded.ToArray());
        var after = reloaded.Pages[0].ExtractPositionedText().Single(run => run.Text == "Z").Bounds.Left;

        Assert.Equal(1, count);
        Assert.Equal(before, after, 6);
    }

    // A zero font size makes the em width zero, which would make every positive gap clear the
    // word-break threshold. The composition rule falls back to the previous run's em instead.
    private const string ZeroSizeRun = "BT /F0 10 Tf 1 0 0 1 72 700 Tm (A) Tj /F0 0 Tf 1 0 0 1 79.67 700 Tm (B) Tj ET";

    [Fact]
    public void FindText_AcrossZeroFontSizeRun_DoesNotInventAWordBreak()
    {
        var loaded = Loaded(ZeroSizeRun);

        Assert.Single(loaded.Pages[0].FindText("AB"));
    }

    // Search measures real glyph widths where extraction estimates half an em per glyph, so
    // the same gap can still read as a word break to one and not the other. That advance
    // model is the deliberate difference between them; the rule applied to it is not.
    [Fact]
    public void ExtractText_EstimatedAdvance_StillBreaksWhereSearchDoesNot()
    {
        var loaded = Loaded(ZeroSizeRun);

        Assert.Equal("A B", loaded.Pages[0].ExtractText());
    }
}
