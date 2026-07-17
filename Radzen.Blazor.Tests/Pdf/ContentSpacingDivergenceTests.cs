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

    // ISO 32000-1 8.4: q/Q save and restore the entire graphics state, text state included.
    // A Tc set inside a q/Q scope therefore does not reach a show after the Q.
    [Fact]
    public void ExtractPositionedText_CharSpacingScopedByQ_DoesNotReachLaterShow()
    {
        var scoped = Loaded("BT /F0 10 Tf q 5 Tc Q 1 0 0 1 72 700 Tm (AB) Tj ET");
        var plain = Loaded("BT /F0 10 Tf 1 0 0 1 72 700 Tm (AB) Tj ET");

        var scopedRun = scoped.Pages[0].ExtractPositionedText().Single();
        var plainRun = plain.Pages[0].ExtractPositionedText().Single();

        Assert.Equal(plainRun.Bounds.Right, scopedRun.Bounds.Right, 6);
    }

    // The replacement's advance compensation is measured against the text state the show
    // actually ran under, so a Tc the Q discarded must not enter the adjustment.
    [Fact]
    public void ReplaceText_CharSpacingScopedByQ_CompensatesWithoutIt()
    {
        var scoped = Loaded("BT /F0 10 Tf q 5 Tc Q 1 0 0 1 72 700 Tm (Hello) Tj (Z) Tj ET");
        var plain = Loaded("BT /F0 10 Tf 1 0 0 1 72 700 Tm (Hello) Tj (Z) Tj ET");

        Assert.Equal(1, scoped.ReplaceText("Hello", "Hi"));
        Assert.Equal(1, plain.ReplaceText("Hello", "Hi"));

        var scopedZ = InterpreterTestSupport.Load(scoped.ToArray()).Pages[0]
            .ExtractPositionedText().Single(run => run.Text == "Z").Bounds.Left;
        var plainZ = InterpreterTestSupport.Load(plain.ToArray()).Pages[0]
            .ExtractPositionedText().Single(run => run.Text == "Z").Bounds.Left;

        Assert.Equal(plainZ, scopedZ, 6);
    }

    // A standalone Tc/Tw is part of the text state the run was drawn under, so it reaches the
    // materialized run. The source operators are copied verbatim either way, so only the
    // model can show the difference.
    [Fact]
    public void MaterializedRun_CapturesPrecedingCharAndWordSpacing()
    {
        var loaded = Loaded("BT /F0 10 Tf 5 Tc 2 Tw 1 0 0 1 72 700 Tm (A B) Tj ET");

        var run = loaded.Pages[0].Content.OfType<TextContent>().Single();

        Assert.Equal(5, run.CharSpacing);
        Assert.Equal(2, run.WordSpacing);
    }

    // An operator's operands are the last n numbers on the frame, so a TL preceded by a
    // stray number takes the trailing one. The two copies of this read disagreed: one took
    // the first number, the other the last.
    [Fact]
    public void NextLine_AfterLeadingWithStrayOperand_UsesTheTrailingNumber()
    {
        var stray = Loaded("BT /F0 10 Tf 99 20 TL 1 0 0 1 72 700 Tm (A) Tj T* (B) Tj ET");
        var plain = Loaded("BT /F0 10 Tf 20 TL 1 0 0 1 72 700 Tm (A) Tj T* (B) Tj ET");

        var strayB = stray.Pages[0].ExtractPositionedText().Single(run => run.Text == "B");
        var plainB = plain.Pages[0].ExtractPositionedText().Single(run => run.Text == "B");

        Assert.Equal(plainB.Bounds.Bottom, strayB.Bounds.Bottom, 6);
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

    [Fact]
    public void ExtractText_AcrossZeroFontSizeRun_AgreesWithFindText()
    {
        var loaded = Loaded(ZeroSizeRun);

        Assert.Equal("AB", loaded.Pages[0].ExtractText());
    }
}
