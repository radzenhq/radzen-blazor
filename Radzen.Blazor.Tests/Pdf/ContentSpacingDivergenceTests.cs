#nullable enable

using System.IO;
using System.Linq;
using Radzen.Documents.Pdf;
using Xunit;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

public class ContentSpacingDivergenceTests
{
    private static PortableDocument Loaded(string streamData)
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
        return PortableDocument.LoadFromStream(input);
    }

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

    // ISO 32000-1 8.4: q/Q save and restore the entire graphics state, text state included
    [Fact]
    public void ExtractPositionedText_CharSpacingScopedByQ_DoesNotReachLaterShow()
    {
        var scoped = Loaded("BT /F0 10 Tf q 5 Tc Q 1 0 0 1 72 700 Tm (AB) Tj ET");
        var plain = Loaded("BT /F0 10 Tf 1 0 0 1 72 700 Tm (AB) Tj ET");

        var scopedRun = scoped.Pages[0].ExtractPositionedText().Single();
        var plainRun = plain.Pages[0].ExtractPositionedText().Single();

        Assert.Equal(plainRun.Bounds.Right, scopedRun.Bounds.Right, 6);
    }

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

    [Fact]
    public void MaterializedRun_CapturesPrecedingCharAndWordSpacing()
    {
        var loaded = Loaded("BT /F0 10 Tf 5 Tc 2 Tw 1 0 0 1 72 700 Tm (A B) Tj ET");

        var run = loaded.Pages[0].Content.OfType<TextContent>().Single();

        Assert.Equal(5, run.CharSpacing);
        Assert.Equal(2, run.WordSpacing);
    }

    [Fact]
    public void NextLine_AfterLeadingWithStrayOperand_UsesTheTrailingNumber()
    {
        var stray = Loaded("BT /F0 10 Tf 99 20 TL 1 0 0 1 72 700 Tm (A) Tj T* (B) Tj ET");
        var plain = Loaded("BT /F0 10 Tf 20 TL 1 0 0 1 72 700 Tm (A) Tj T* (B) Tj ET");

        var strayB = stray.Pages[0].ExtractPositionedText().Single(run => run.Text == "B");
        var plainB = plain.Pages[0].ExtractPositionedText().Single(run => run.Text == "B");

        Assert.Equal(plainB.Bounds.Bottom, strayB.Bounds.Bottom, 6);
    }

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
