#nullable enable

using System;
using System.IO;
using System.Text;
using Radzen.Documents.Pdf;
using Xunit;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

// ISO 32000-1 Table 111 (/Widths): MissingWidth is used for codes outside FirstChar..LastChar; Table 122 defaults MissingWidth to 0.
public class EstimatedTextGeometryTests
{
    private static readonly PdfRect TrueXBounds = new(50, 700, 56.67, 710);

    private static PortableDocument LoadedDocument(bool widthForW, bool descriptor = false, int? missingWidth = null)
    {
        const string stream = "BT /F1 10 Tf 10 700 Td (WWWWX) Tj ET";
        var widths = widthForW ? "/FirstChar 87 /LastChar 88 /Widths [1000 667]" : "/FirstChar 88 /LastChar 88 /Widths [667]";
        var last = descriptor ? 6 : 5;
        var pdf = new FixturePdf()
            .Append("%PDF-1.7\n")
            .Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n")
            .Object(2, "2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n")
            .Object(3, "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] "
                + "/Resources << /Font << /F1 5 0 R >> >> /Contents 4 0 R >>\nendobj\n")
            .Object(4, $"4 0 obj\n<< /Length {stream.Length} >>\nstream\n{stream}\nendstream\nendobj\n")
            .Object(5, "5 0 obj\n<< /Type /Font /Subtype /TrueType /BaseFont /Widthless /Encoding /WinAnsiEncoding "
                + widths + (descriptor ? " /FontDescriptor 6 0 R" : string.Empty) + " >>\nendobj\n");

        if (descriptor)
        {
            pdf.Object(6, "6 0 obj\n<< /Type /FontDescriptor /FontName /Widthless /Flags 32 /ItalicAngle 0 "
                + "/Ascent 700 /Descent -200 /CapHeight 700 /StemV 80 /FontBBox [0 -200 1000 900]"
                + (missingWidth is { } value ? $" /MissingWidth {value}" : string.Empty) + " >>\nendobj\n");
        }

        var xref = pdf.Position;
        pdf.Append($"xref\n0 {last + 1}\n").Append(FixturePdf.Entry20(0, 65535, 'f'));
        for (var number = 1; number <= last; number++)
        {
            pdf.Append(FixturePdf.Entry20(pdf.OffsetOf(number)));
        }

        pdf.Append($"trailer\n<< /Size {last + 1} /Root 1 0 R >>\nstartxref\n" + xref + "\n%%EOF\n");
        using var input = new MemoryStream(pdf.ToArray());
        return PortableDocument.LoadFromStream(input);
    }

    private static string SavedContent(PortableDocument document)
        => Encoding.Latin1.GetString(InterpreterTestSupport.PageContentBytes(document.ToArray(), 0));

    [Fact]
    public void FindText_WithEveryWidth_IsExactAndNotEstimated()
    {
        var hit = Assert.Single(LoadedDocument(widthForW: true).Pages[0].FindText("X"));

        Assert.False(hit.GeometryEstimated);
        Assert.Equal(TrueXBounds.Left, hit.Bounds.Left, 2);
        Assert.Equal(TrueXBounds.Right, hit.Bounds.Right, 2);
    }

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

    [Fact]
    public void Redact_AreaOffTheEstimatedRunsBaseline_Succeeds()
    {
        var document = LoadedDocument(widthForW: false);

        document.Pages[0].Redact([PdfRect.FromSize(10, 10, 50, 50)]);

        Assert.Contains("(WWWWX)", SavedContent(document), StringComparison.Ordinal);
    }

    [Fact]
    public void Redact_AreaBehindTheEstimatedRunsOrigin_Succeeds()
    {
        var document = LoadedDocument(widthForW: false);

        document.Pages[0].Redact([new PdfRect(0, 700, 9, 710)]);

        Assert.Contains("(WWWWX)", SavedContent(document), StringComparison.Ordinal);
    }

    [Fact]
    // ISO 32000-1 Table 111 (/Widths): MissingWidth is used for codes outside FirstChar..LastChar.
    public void FindText_WithMissingWidth_UsesItRatherThanEstimating()
    {
        var hit = Assert.Single(LoadedDocument(widthForW: false, descriptor: true, missingWidth: 1000).Pages[0].FindText("X"));

        Assert.False(hit.GeometryEstimated);
        Assert.Equal(TrueXBounds.Left, hit.Bounds.Left, 2);
        Assert.Equal(TrueXBounds.Right, hit.Bounds.Right, 2);
    }

    [Fact]
    // ISO 32000-1 Table 122: MissingWidth is Optional, default value 0.
    public void FindText_WithDescriptorButNoMissingWidth_DefaultsToZeroRatherThanEstimating()
    {
        var hit = Assert.Single(LoadedDocument(widthForW: false, descriptor: true).Pages[0].FindText("X"));

        Assert.False(hit.GeometryEstimated);
        Assert.Equal(10.0, hit.Bounds.Left, 2);
    }
}
