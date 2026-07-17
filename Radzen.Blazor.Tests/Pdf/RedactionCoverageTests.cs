#nullable enable

using System;
using System.IO;
using System.Text;
using Radzen.Documents.Pdf;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// Redaction is documented as irreversibly removing intersecting content, so every painting
// operator has to be accounted for: an inline image or a shading that intersects a region
// must not survive, and one that does not intersect must not be disturbed.
public class RedactionCoverageTests
{
    private const string InlineImage = "BI /W 1 /H 1 /BPC 8 /CS /G ID * EI";

    private static Document LoadedDocument(string streamData)
    {
        var contentObject = $"4 0 obj\n<< /Length {streamData.Length} >>\nstream\n{streamData}\nendstream\nendobj\n";
        var pdf = new FixturePdf()
            .Append("%PDF-1.7\n")
            .Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n")
            .Object(2, "2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n")
            .Object(3, "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] "
                + "/Resources << /Shading << /Sh0 5 0 R >> >> /Contents 4 0 R >>\nendobj\n")
            .Object(4, contentObject)
            .Object(5, "5 0 obj\n<< /ShadingType 2 /ColorSpace /DeviceGray /Coords [0 0 1 1] "
                + "/Function << /FunctionType 2 /Domain [0 1] /C0 [0] /C1 [1] /N 1 >> >>\nendobj\n");
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

    private static string SavedContent(Document document)
        => Encoding.Latin1.GetString(InterpreterTestSupport.PageContentBytes(document.ToArray(), 0));

    // The image maps the unit square through "100 0 0 100 10 10 cm", so it covers
    // (10,10)-(110,110). The trailing rectangle is a second element well away from it, so
    // removing either one still leaves the page re-emitting through ContentEditor.
    private static Document LoadedInlineImageDocument()
        => LoadedDocument($"q 100 0 0 100 10 10 cm {InlineImage} Q 450 450 10 10 re f");

    // A redaction region is PDF user space, not top-left: on a 792pt page it must remove the
    // rectangle sharing its coordinates and leave the one mirrored across the centre line.
    [Fact]
    public void Redact_RegionIsMeasuredInPdfUserSpace()
    {
        var loaded = LoadedDocument("100 100 50 50 re f 100 642 50 50 re f");

        loaded.Pages[0].Redact(new[] { PdfRect.FromSize(100, 100, 50, 50) });
        var content = SavedContent(loaded);

        Assert.DoesNotContain("100 100 50 50 re", content, StringComparison.Ordinal);
        Assert.Contains("100 642 50 50 re", content, StringComparison.Ordinal);
    }

    [Fact]
    public void Redact_RegionIntersectingInlineImage_RemovesTheImage()
    {
        var loaded = LoadedInlineImageDocument();

        loaded.Pages[0].Redact(new[] { PdfRect.FromSize(0, 0, 200, 200) });

        Assert.DoesNotContain("BI", SavedContent(loaded), StringComparison.Ordinal);
    }

    [Fact]
    public void Redact_RegionNotIntersectingInlineImage_PreservesTheImage()
    {
        var loaded = LoadedInlineImageDocument();

        loaded.Pages[0].Redact(new[] { PdfRect.FromSize(400, 400, 100, 100) });
        var content = SavedContent(loaded);

        Assert.Contains(InlineImage, content, StringComparison.Ordinal);
        Assert.DoesNotContain("re", content, StringComparison.Ordinal);
    }

    [Fact]
    public void Redact_RegionIntersectingClippedShading_FailsLoud()
    {
        var loaded = LoadedDocument("q 10 10 100 100 re W n /Sh0 sh Q");

        var exception = Assert.Throws<NotSupportedException>(
            () => loaded.Pages[0].Redact(new[] { PdfRect.FromSize(0, 0, 200, 200) }));

        Assert.Contains("sh", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Redact_RegionNotIntersectingClippedShading_PreservesTheShading()
    {
        var loaded = LoadedDocument("q 10 10 100 100 re W n /Sh0 sh Q");

        loaded.Pages[0].Redact(new[] { PdfRect.FromSize(400, 400, 100, 100) });

        Assert.Contains("sh", SavedContent(loaded), StringComparison.Ordinal);
    }

    [Fact]
    public void Redact_UnclippedShading_FailsLoud()
    {
        var loaded = LoadedDocument("/Sh0 sh");

        var exception = Assert.Throws<NotSupportedException>(
            () => loaded.Pages[0].Redact(new[] { PdfRect.FromSize(400, 400, 100, 100) }));

        Assert.Contains("sh", exception.Message, StringComparison.Ordinal);
    }
}
